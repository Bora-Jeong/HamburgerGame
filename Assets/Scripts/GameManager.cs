﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Dialogue
{
    public int bgIndex; // 배경 사진 몇번째
    public string content;
}

public enum Ingredient
{
    TopBread,
    Tomato,
    Cabbage,
    Cheese,
    Patty,
    BottomBread
}

public class GameManager : Singleton<GameManager>
{
    [SerializeField] GameObject _lobbyPanel;
    [SerializeField] DialoguePanel _dialoguePanel;
    [SerializeField] GameObject _gamePanel;
    [SerializeField] GameEndPanel _gameEndPanel;
    [SerializeField] Sprite[] _ingredientSprites;
    [SerializeField] Sprite[] _aiIngredientSprites;

    [Header("Lobby")]
    [SerializeField] Text _stageText;

    [Header("Prologue")]
    [SerializeField] Sprite[] _prologueBG;
    [SerializeField] Dialogue[] _dialogues;

    [Header("Game")]
    [SerializeField] Text _dayText;
    [SerializeField] Slider _timeSlider;
    [SerializeField] Text _timeText;
    [SerializeField] Text _playerScoreText;
    [SerializeField] Text _aiScoreText;
    [SerializeField] Button HammerButton;

    [SerializeField] Transform _recipeRoot;
    [SerializeField] Transform _aiRecipeRoot;
    [SerializeField] GameObject _recipe;
    [SerializeField] Sprite _aiRecipe;
    [SerializeField] AITalk _aiTalk;
    [SerializeField] AI _ai;

    [SerializeField] GameObject _intro;
    [SerializeField] Text _introText;

    private float _totalTime;
    private float _remainTime;
    private int _playerScore;
    private int _aiScore;

    private Queue<Hamburger> _recipeQ = new Queue<Hamburger>(); // 플레이어가 만들어야 하는 주문서들

    public Hamburger playerHamburger;

    private Queue<Hamburger> _aiRecipeQ = new Queue<Hamburger>(); // AI의 주문서들

    public Hamburger aiHamburger;

    private Coroutine _prologueCor = null;
    private Coroutine _introCor = null;

    private readonly float _recipeDistance = 170f;
    private readonly float _aiTalkTerm = 8f; // 8초에 한번씩 도발
    private float _aiTalkTime; // AI 말하는 타이머
    public bool isPlaying { get; private set; } // 게임 중?
    private int _hammerChance = 1; // 깡 찬스
    private int _aiDisturbCount = 1;
    private float _aiSpeed = 4f;

    public int playerScore
    {
        get => _playerScore;
        set
        {
            _playerScore = value;
            _playerScoreText.text = $"{_playerScore}";
            if(_aiScore < _playerScore)
            {
                if(_aiDisturbCount > 0 && Random.Range(0,5) < 1)
                {
                    HideRecipe(); // 방해공작
                }
            }
        }
    }

    public int aiScore
    {
        get => _aiScore;
        set
        {
            _aiScore = value;
            _aiScoreText.text = $"{_aiScore}";
            if(_aiScore % 3 == 0 && _aiScore !=0)
            {
                _aiTalk.SuccessShow();
                _aiTalkTime = _aiTalkTerm;
            }
        }
    }

    public float remainTime
    {
        get => _remainTime;
        set
        {
            _remainTime = value;
            _timeText.text = $"{(int)_remainTime}초";
            _timeSlider.value = _remainTime / _totalTime;
        }
    }

    public int stage
    {
        get => PlayerPrefs.GetInt("Stage", 1);
        set
        {
            PlayerPrefs.SetInt("Stage", value);
        }
    }

    private void Awake()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.SetResolution(1920, 1080, true);

        _stageText.text = stage.ToString();
        _lobbyPanel.gameObject.SetActive(true);
        _dialoguePanel.gameObject.SetActive(false);
        _gamePanel.gameObject.SetActive(false);
    }

    public void StartGameButton()
    {
        AudioManager.instance.ClickSound();
        _lobbyPanel.gameObject.SetActive(false);
        _prologueCor = StartCoroutine(StartPrologue());
    }

    IEnumerator StartPrologue()  // 프롤로그 시작
    {
        _gamePanel.gameObject.SetActive(false);
        _dialoguePanel.gameObject.SetActive(true);

        for (int i = 0; i < _dialogues.Length; i++)
        {
            _dialoguePanel.Set(_prologueBG[_dialogues[i].bgIndex], _dialogues[i].content);
            yield return new WaitForSeconds(3f);
        }

        _introCor = StartCoroutine(StartIntroduction());

        _prologueCor = null;
    }

    private IEnumerator StartIntroduction() // 게임 설명 시작
    {
        _dialoguePanel.gameObject.SetActive(false);
        _gamePanel.gameObject.SetActive(true);

        _intro.SetActive(true);
        string content = _introText.text;
        _introText.text = string.Empty;

        for (int i = 0; i < content.Length; i++)
        {
            _introText.text += content[i];
            AudioManager.instance.TypeSound();
            yield return new WaitForSeconds(0.05f);
        }

        yield return new WaitForSeconds(0.2f);

        _intro.SetActive(false);
        RoundStart(int.Parse(_stageText.text));

        _introCor = null;
    }


    public void OnSkipPrologueButton()
    {
        AudioManager.instance.ClickSound();
        if(_prologueCor != null)
        {
            StopCoroutine(_prologueCor);
            _prologueCor = null;
            _introCor = StartCoroutine(StartIntroduction());
        }
    }

    public void OnSkipIntroButton()
    {
        AudioManager.instance.ClickSound();
        if(_introCor != null)
        {
            StopCoroutine(_introCor);
            _introCor = null;
            _intro.SetActive(false);
            RoundStart(int.Parse(_stageText.text));
        }
    }

    public void nextRound()
    {
        int cur = int.Parse(_stageText.text);
        if(cur == stage)
            stage++;
        RoundStart(cur+1);
    }

    public void Restart()
    {
        int cur = int.Parse(_stageText.text);
        RoundStart(cur);
    }

    private void RoundStart(int stage, float time = 60) // 라운드 시작
    {
        _dayText.text = $"{stage}라운드";
        _totalTime = time;
        remainTime = _totalTime;
        playerScore = 0;
        aiScore = 0;
        _aiTalkTime = _aiTalkTerm;
        _aiSpeed = Mathf.Min(4f * Mathf.Pow(1.2f,  stage - 1), 10f);
        _hammerChance = 1;
        _aiDisturbCount = 1;
        HammerButton.interactable = true;
        playerHamburger.Discard();
        aiHamburger.Discard();
        RefreshRecipe();

        isPlaying = true;
        _ai.StartWork(_aiSpeed);
        AudioManager.instance.restartBackgroundMusic();
        StartCoroutine(GameSchedulling());
    }

    IEnumerator GameSchedulling()
    {
        while (remainTime > 0)
        {
            remainTime -= Time.deltaTime;
            _aiTalkTime -= Time.deltaTime;
            if (_aiTalkTime <= 0) // AI가 도발하는 멘트
            {
                if(_ai.isWorking) _aiTalk.Show();
                _aiTalkTime = _aiTalkTerm;
            }
            
            yield return null;
        }

        GameOver();
    }

    private void GameOver() // 게임 오버
    {
        isPlaying = false;
        _ai.StopWork();
        _gameEndPanel.gameObject.SetActive(true);
        _gameEndPanel.SetText(stage, playerScore, aiScore);
    }

    private void HideRecipe()
    {
        print("방해공작");
        _aiDisturbCount--;
        _recipeQ.Peek().GetComponentInParent<Recipe>().Hide();
    }

    private void RefreshRecipe()
    {
        while (_recipeQ.Count > 0)
        {
            Hamburger temp = _recipeQ.Dequeue();
            DestroyImmediate(temp.transform.parent.gameObject);
        }
        while (_aiRecipeQ.Count > 0)
        {
            Hamburger temp = _aiRecipeQ.Dequeue();
            DestroyImmediate(temp.transform.parent.gameObject);
        }

        _recipeRoot.GetComponent<HorizontalLayoutGroup>().enabled = true;
        for (int i = 0; i < 40; i++) // 라운드 시작시 일단 40개 레시피 로드해놓음
        {
            Hamburger hamburger = GetRandomHamburger();
            GameObject recipe = Instantiate(_recipe, _recipeRoot);
            hamburger.transform.SetParent(recipe.transform);
            hamburger.transform.localScale = Vector3.one;
            hamburger.transform.localPosition = new Vector3(0, -50, 0);
            _recipeQ.Enqueue(hamburger);

            GameObject copy = Instantiate(recipe, _aiRecipeRoot);
            copy.GetComponent<Image>().sprite = _aiRecipe;
            copy.transform.localScale = Vector3.one;
            copy.transform.localPosition = new Vector3(0, -30, 0);
            Hamburger aiBurger = copy.GetComponentInChildren<Hamburger>();
            aiBurger.transform.localScale = Vector3.one * 0.5f;
            aiBurger.transform.localPosition = new Vector3(0, -20, 0);
            aiBurger.ingredients = new Queue<Ingredient>(hamburger.ingredients);
            _aiRecipeQ.Enqueue(aiBurger);
        }
    }

    public void OnServingButton()
    {
        Queue<Ingredient> player = playerHamburger.ingredients;

        Hamburger recipe = _recipeQ.Peek(); // 레시피 제일 앞 버거
        Queue<Ingredient> backup = new Queue<Ingredient>(recipe.ingredients);

        bool success = true;
        while (player.Count > 0 && recipe.ingredients.Count > 0)
        {
            //Debug.Log($"Player {player.Peek()}  vs {recipe.ingredients.Peek()}");
            if (player.Dequeue() != recipe.ingredients.Dequeue())
            {
                success = false;
                break;
            }
        }

        if (player.Count > 0 || recipe.ingredients.Count > 0)
            success = false;

        if (success) // 성공시
        {
            _recipeQ.Dequeue();
            playerScore++;
            AudioManager.instance.ServeSound();

            _recipeRoot.GetComponent<HorizontalLayoutGroup>().enabled = false;
            Transform bill = recipe.transform.parent;
            bill.SetParent(_gamePanel.transform);
            LeanTween.moveY(bill.gameObject, bill.position.y + 100f, 0.3f).setDestroyOnComplete(true);
            for(int i = 0; i < _recipeRoot.childCount; i++)
            {
                Transform child = _recipeRoot.GetChild(i);
                LeanTween.moveX(child.gameObject, child.transform.position.x - _recipeDistance, 0.3f);
            }

        }
        else
            recipe.ingredients = backup;

        OnDumpButton(); // 플레이어 큐, 쟁반 클리어
    }


    public void OnDumpButton()
    {
        AudioManager.instance.DumpSound();
       playerHamburger.ingredients.Clear(); // 플레이어 큐 clear
        for (int i = playerHamburger.transform.childCount - 1; i >= 0; i--) // 쟁반 클리어
            Destroy(playerHamburger.transform.GetChild(i).gameObject);
    }

    public void OnHammerButton()
    {
        if(_hammerChance > 0)
        {
            _ai.Pause();
            _hammerChance--;
            _aiTalk.PauseShow();
        }
    }

    public void ServeHamburger_ai()
    {
        Hamburger recipe = _aiRecipeQ.Dequeue();

        aiHamburger.Discard();
        
        aiScore++;
        Destroy(recipe.transform.parent.gameObject);

    }

    private Hamburger GetRandomHamburger()
    {
        Hamburger hamburger = new GameObject("Hamburger").AddComponent<Hamburger>();
        int count = Random.Range(stage + 1, stage + 3); // 1일차 최소 2 , 최대 3개
        count = Mathf.Min(count, 5);
        hamburger.StackIngredientUI(Ingredient.BottomBread);
        for (int i = 0; i < count; i++)
            hamburger.StackIngredientUI((Ingredient)Random.Range(1, 5));
        hamburger.StackIngredientUI(Ingredient.TopBread);
        return hamburger;
    }
    

    public Hamburger GetAiRecipe()
    {
        return _aiRecipeQ.Peek();
    }

    public Sprite GetIngredientSprite(Ingredient ingredient, bool isAI = false) // 재료 사진 얻는 함수!!!
    {
        if(isAI)  return _aiIngredientSprites[(int)ingredient];
        else  return _ingredientSprites[(int)ingredient];
    }

    public void OnPrevStageButton()
    {
        AudioManager.instance.ClickSound();
        int cur = int.Parse(_stageText.text);
        cur = Mathf.Max(cur - 1, 1);
        _stageText.text = cur.ToString();
    }
    public void OnNextStageButton()
    {
        AudioManager.instance.ClickSound();
        int cur = int.Parse(_stageText.text);
        cur = Mathf.Min(cur+1, stage);
        _stageText.text = cur.ToString();
    }

    public void StopGame()
    {
        StopAllCoroutines();
        _gamePanel.SetActive(false);
        _gameEndPanel.gameObject.SetActive(false);
        _ai.StopWork();
        _stageText.text = stage.ToString();
        _lobbyPanel.SetActive(true);
    }

}
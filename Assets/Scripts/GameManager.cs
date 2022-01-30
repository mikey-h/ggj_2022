using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    static public GameManager m_instance;
    // Start is called before the first frame update

    public GameObject m_failUI;
    public GameObject m_successUI;
    public GameObject m_levelNameUI;
    public GameObject m_intro;
    public Text m_levelNumText;
    public Text m_levelTitleText;
    public Image m_blackScreen;
    public float m_fadeSpeed = 1.0f;

    float m_extraStartDelay = 5.0f;


    enum GameState
    {
        Playing,
        ShowSuccess,
        ShowFail,
    };

    enum FadeState
    {
        Black,
        ToBlack,
        ToClear,
        Clear,
    };

    GameState m_state;
    FadeState m_fadeState;
    float m_fadeAlpha;


    void Awake()
    {
        m_instance = this;

        m_fadeAlpha = 1.0f;
        m_blackScreen.gameObject.SetActive(true);
        SetFadeAlpha();
        m_intro.SetActive(true);
    }

    public void OpenMusicCreditPage()
    {
        Application.OpenURL("https://freemusicarchive.org/music/techtheist/fma2021-part-2/the-mirror-of-sound");
    }

    void SetFadeAlpha()
    {
        m_blackScreen.color = new Color(0.0f, 0.0f, 0.0f, m_fadeAlpha);
    }
    public void ShowLevelText(int num)
    {
        m_levelNumText.text = "Level "+num;
        switch(num)
        {
            // duality
            // always dark, or always light.
            // 
            case 1:
                m_levelTitleText.text = "In which our hero discovers\na balance between fire and ice";
                break;
            case 2:
                m_levelTitleText.text = "How can something so hot\nneed something so cold?";
                break;
            case 3:
                m_levelTitleText.text = "Where our hero learns\nto navigate between extremes";
                break;
            case 4:
                m_levelTitleText.text = "Our hero find the obvious way\nis not always the way to find balance";
                break;
            case 5:
                m_levelTitleText.text = "Thank you for playing\nlet me know if you'd like more puzzles";
                break;

            default:
            break;
        }

        m_levelNameUI.SetActive(true);
        Invoke("HideLevelText", 6.0f);
    }

    void HideLevelText()
    {
        m_levelNameUI.SetActive(false);
    }

    void Start()
    {
        m_state = GameState.Playing;
    }

    // Update is called once per frame
    void Update()
    {
        switch(m_state)
        {
            case GameState.ShowSuccess:
                if (Input.GetKeyDown("space"))
                {
                    StartNextLevel();
                    m_state = GameState.Playing;
                }
                break;

            case GameState.ShowFail:
                if (Input.GetKeyDown("space"))
                {
                    RestartLevel();
                    m_state = GameState.Playing;
                }
                break;

            default:
                break;
        }

        switch(m_fadeState)
        {

            case FadeState.ToBlack:
                m_fadeAlpha += m_fadeSpeed * Time.deltaTime;
                if(m_fadeAlpha >= 1.0f)
                {
                    m_fadeState = FadeState.Black;
                    m_fadeAlpha = 1.0f;
                }
                SetFadeAlpha();
                break;

            case FadeState.ToClear:
                if(m_extraStartDelay>0.0f)
                {
                    m_extraStartDelay -= Time.deltaTime;
                    if(m_extraStartDelay <=0.0f)
                    {
                        m_intro.SetActive(false);
                        ShowLevelText(Map.m_instance.m_levelNum);
                    }
                }

                if(m_extraStartDelay <=0.0f)
                {
                    m_fadeAlpha -= m_fadeSpeed * Time.deltaTime;
                    if(m_fadeAlpha <= 0.0f)
                    {
                        m_fadeState = FadeState.Clear;
                        m_fadeAlpha = 0.0f;
                        m_blackScreen.gameObject.SetActive(false);
                    }
                }
                SetFadeAlpha();
                break;

            case FadeState.Black:
            case FadeState.Clear:
            default:
                break;
        }


        if(Input.anyKey)
        {
            m_levelNameUI.SetActive(false);
        }
    }

    public void Failed()
    {
        
    }

    void NextLevelAfterFadeToBlack()
    {
        Map.m_instance.StartNextLevel();
    }

    void ResetAfterFadeToBlack()
    {
        Map.m_instance.ResetLevel();
    }

    public void StartNextLevel()
    {
        m_successUI.SetActive(false);
        m_failUI.SetActive(false);

        FadeToBlack();

        Invoke("NextLevelAfterFadeToBlack", 1.0f);
    }

    public void RestartLevel()
    {
        m_successUI.SetActive(false);
        m_failUI.SetActive(false);

        FadeToBlack();

        Invoke("ResetAfterFadeToBlack", 1.0f);
    }

    void FadeToBlack()
    {
        m_fadeState = FadeState.ToBlack;
        m_blackScreen.gameObject.SetActive(true);
    }

    public void FadeToClear()
    {
        m_fadeState = FadeState.ToClear;
    }

    public void NextLevel()
    {
        m_successUI.SetActive(true);
        m_state = GameState.ShowSuccess;
    }

    public void FailLevel()
    {
        m_failUI.SetActive(true);
        m_state = GameState.ShowFail;
    }
}

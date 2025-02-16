using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ViewManager : MonoBehaviour
{
    private static ViewManager s_instance;

    [SerializeField] private View _startingView;
    [SerializeField] private View[] _views;

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _buttonClickSound; 

    private View _currentView;

    private readonly Stack<View> _history = new Stack<View>();
    public static T GetView<T>() where T : View
    {
        for (int i = 0; i < s_instance._views.Length; i++)
        {
            if (s_instance._views[i] is T tView)
            {
                return tView;
            }
        }

        return null;
    }

    public static void Show<T>(bool remember = true, AudioClip soundEffect = null) where T : View
    {
        s_instance?.PlaySound(soundEffect);

        for (int i = 0; i < s_instance._views.Length; i++)
        {
            if (s_instance._views[i] is T)
            {
                if (s_instance._currentView != null)
                {
                    if (remember)
                    {
                        s_instance._history.Push(s_instance._currentView);
                    }

                    s_instance._currentView.Hide();
                }

                s_instance._views[i].Show();
                s_instance._currentView = s_instance._views[i];
            }
        }
    }

    public static void Show(View view, bool remember = true, AudioClip soundEffect = null)
    {
        s_instance?.PlaySound(soundEffect);

        if (s_instance._currentView != null)
        {
            if (remember)
            {
                s_instance._history.Push(s_instance._currentView);
            }

            s_instance._currentView.Hide();
        }

        view.Show();
        s_instance._currentView = view;
    }

    public static void ShowLast(AudioClip soundEffect = null)
    {
        if (s_instance._history.Count != 0)
        {
            Show(s_instance._history.Pop(), false, soundEffect);
        }
    }

    public void LoadGame()
    {
        PlaySound(_buttonClickSound); 
        StartCoroutine(LoadGameWithDelay());
    }

    private IEnumerator LoadGameWithDelay()
    {
        yield return new WaitForSeconds(0.5f); 
        SceneManager.LoadScene("Game");      
    }
    
    private void PlaySound(AudioClip soundEffect)
    {
        if (_audioSource != null)
        {
            if (soundEffect != null)
            {
                _audioSource.PlayOneShot(soundEffect);
            }
            else if (_buttonClickSound != null)
            {
                _audioSource.PlayOneShot(_buttonClickSound);
            }
        }
    }

    private void Awake() => s_instance = this;

    private void Start()
    {
        Time.timeScale = 0f;
        
        for (int i = 0; i < _views.Length; i++)
        {
            _views[i].Initialize();
            _views[i].Hide();
        }

        if (_startingView != null)
        {
            Show(_startingView, true);
        }
    }

    public void QuitGame()
    {
        PlaySound(_buttonClickSound); 
        StartCoroutine(QuitGameWithDelay());
    }

    private IEnumerator QuitGameWithDelay()
    {
        yield return new WaitForSeconds(0.5f); 
        Application.Quit();    
    }
}
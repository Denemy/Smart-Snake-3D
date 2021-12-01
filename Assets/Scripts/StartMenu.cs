using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public Button Play;
    public Button Options;
    public Button Quit;

    void Start()
    {
        if (!PlayerPrefs.HasKey("NumberOfSnakes"))                              // Если нет параметра - 
            PlayerPrefs.SetInt("NumberOfSnakes", 2);                            // создаем его и устанавливаем значение по умолчанию.
        if (!PlayerPrefs.HasKey("Food"))                                        // Если нет параметра - 
            PlayerPrefs.SetInt("Food", 10);                                     // создаем его и устанавливаем значение по умолчанию.
        if (!PlayerPrefs.HasKey("Speed"))                                       // Если нет параметра - 
            PlayerPrefs.SetInt("Speed", 2);                                     // создаем его и устанавливаем значение по умолчанию.
        if (!PlayerPrefs.HasKey("Sound"))                                       // Если нет параметра - 
            PlayerPrefs.SetInt("Sound", 1);                                     // создаем его и устанавливаем значение по умолчанию.
        if (!PlayerPrefs.HasKey("Music"))                                       // Если нет параметра - 
            PlayerPrefs.SetInt("Music", 1);                                     // создаем его и устанавливаем значение по умолчанию.
        PlayerPrefs.Save();                                                     // Сохраняем все измененные настройки.

        Global.numberOfSnakes = PlayerPrefs.GetInt("NumberOfSnakes");
        Global.food = PlayerPrefs.GetInt("Food");
        Global.speed = PlayerPrefs.GetInt("Speed");
        Global.sound = (PlayerPrefs.GetInt("Sound") == 1) ? true : false;
        Global.music = (PlayerPrefs.GetInt("Music") == 1) ? true : false;
    }

    void Update()
    {
        
    }


    // ======================= Старт игры ========================
    public void GamePlay()
    {
        SceneManager.LoadScene("Main");
    }
    // -----------------------------------------------------------


    // ====================== Настройки игры =====================
    public void GameOptions()
    {
        SceneManager.LoadScene("Options");
    }
    // -----------------------------------------------------------


    // ====================== Выход из игры ======================
    public void GameQuit ()
    {
        Application.Quit();
    }
    // -----------------------------------------------------------
}

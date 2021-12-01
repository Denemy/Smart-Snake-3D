using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Options : MonoBehaviour
{
    public Scrollbar scrollbarSnake;
    public Scrollbar scrollbarFood;
    public Scrollbar scrollbarSpeed;
    public Text textSnake;
    public Text textFood;
    public Text textSpeed;
    public Toggle toggleSound;
    public Toggle toggleMusic;

    private int snake;
    private int food;
    private int speed;
    private bool sound;
    private bool music;


    // =========================== Начало =================================
    void Start()
    {
        snake = PlayerPrefs.GetInt("NumberOfSnakes");
        food = PlayerPrefs.GetInt("Food");
        speed = PlayerPrefs.GetInt("Speed");
        sound = (PlayerPrefs.GetInt("Sound") == 1) ? true : false;
        music = (PlayerPrefs.GetInt("Music") == 1) ? true : false;

        textSnake.text = snake.ToString();                              // Вписываем на ползунок.
        scrollbarSnake.value = 0.2f * (snake - 1);                      // Позиция ползунка.

        textFood.text = food.ToString();                                // Вписываем на ползунок.
        scrollbarFood.value = 0.1f * (food - 10);                       // Позиция ползунка.
        
        textSpeed.text = speed.ToString();                              // Вписываем на ползунок.
        scrollbarSpeed.value = (1f / 9) * (speed - 1);                  // Позиция ползунка.

        toggleSound.isOn = sound;                                       // Переключатель звука.
        toggleMusic.isOn = music;                                       // Переключатель музыки.
    }
    // --------------------------------------------------------------------


    // ======================= Применение настроек ========================
    public void OptionsDone()
    {
        Global.numberOfSnakes = snake;
        Global.food = food;
        Global.speed = (float)speed;
        Global.music = music;
        Global.sound = sound;

        PlayerPrefs.SetInt("NumberOfSnakes", snake);                                // Сохраняем значение.
        PlayerPrefs.SetInt("Food", food);                                           // Сохраняем значение.
        PlayerPrefs.SetInt("Speed", speed);                                         // Сохраняем значение.
        if (sound) PlayerPrefs.SetInt("Sound", 1); 
        else PlayerPrefs.SetInt("Sound", 0);                                        // Сохраняем значение.
        if (music) PlayerPrefs.SetInt("Music", 1);
        else PlayerPrefs.SetInt("Music", 0);                                        // Сохраняем значение.
        PlayerPrefs.Save();                                                         // Сохраняем все измененные настройки.

        SceneManager.LoadScene("Start Menu");                                       // Переходим в стартовое меню.
    }
    // --------------------------------------------------------------------


    // ========================= Отмена настроек ==========================
    public void OptionsCancel()
    {
        SceneManager.LoadScene("Start Menu");
    }
    // --------------------------------------------------------------------


    // ==================== Настройка количества змеек ====================
    public void ChangeSnake()
    {
        snake = (int)((scrollbarSnake.value * 5) + 1);
        textSnake.text = snake.ToString();
    }
    // --------------------------------------------------------------------


    // ==================== Настройка количества еды ======================
    public void ChangeFood()
    {
        food = (int)((scrollbarFood.value * 10) + 10);
        textFood.text = food.ToString();
    }
    // --------------------------------------------------------------------


    // ==================== Настройка скорости змеек ======================
    public void ChangeSpeed()
    {
        speed = (int)((scrollbarSpeed.value * 9) + 1);
        textSpeed.text = speed.ToString();
    }
    // --------------------------------------------------------------------


    // ========================= Выключатель звука ========================
    public void ChangeSound()
    {
        sound = toggleSound.isOn;
    }
    // --------------------------------------------------------------------


    // ========================= Выключатель музыки =======================
    public void ChangeMusic()
    {
        music = toggleMusic.isOn;
    }
    // --------------------------------------------------------------------

}

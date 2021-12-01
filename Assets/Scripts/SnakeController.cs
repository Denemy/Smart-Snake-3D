using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SnakeController : MonoBehaviour
{
    const int SNAKE_MAX_LEN = 10000;                                        // Максимальная длина змейки в шагах.

    public GameObject foodObj;                                              // Ссылка на префаб фрукта.
    public GameObject snakeHead;                                            // Ссылка на префаб головы.
    public GameObject snakePart;                                            // Ссылка на префаб части змеи.
    public GameObject snakeWinner;                                          // Ссылка на победителя.
    public Toggle toggleSaM;                                                // Ссылка на переключатель звука.
    public Toggle togglePause;                                              // Ссылка на переключатель паузы.
    public Button Restart;                                                  // Ссылка на кнопку рестарта.
    public GameObject WinnerHead;                                           // Ссылка на спрайт победителя.
    public GameObject[] SnakeHead;                                          // Ссылка на картинки голов змеек.
    public Sprite[] SpriteHead;                                             // Ссылка на спрайты голов змеек.
    public Sprite[] SpriteHeadSleep;                                        // Ссылка на спрайты голов спящих змеек.
    public Text[] Score;                                                    // Ссылка на текст очков.

    private int snakeNumber;                                                // Количество змеек (1..6).
    private int food;                                                       // Количество еды (10..20).
    private Vector3 pos;
    private int curMicroStep;                                               // Микрошаг.
    private float moveDelay;                                                // Интервал в секундах между обновлениями положения объектов.
    private bool sound, music;                                              // Звук и музыка.
    private bool gamePaused;                                                // Пауза.
    private int[] SnakeSortLength = new int[6];                             // Массив с отсортированными по длине змейками.

    private Color[] col =
    {
        new Color(1f,   0f, 0f, 1f),       // красный.
        new Color(1f,   1f, 0f, 1f),       // желтый.
        new Color(0f,   1f, 0f, 1f),       // зеленый.
        new Color(0f,   1f, 1f, 1f),       // голубой.
        new Color(0f,   0f, 1f, 1f),       // синий.
        new Color(1f,   0f, 1f, 1f)        // фиолетовый.
    };

    enum Direction1D : int { X, Y, Z };
    enum Direction2D : int { XY, XZ, YX, YZ, ZX, ZY };
    enum Direction3D : int { XYZ, XZY, YXZ, YZX, ZXY, ZYX };

    private int[] PathOrder = new int[6];                                // Порядок предпочтительных маршрутов.

    private Vector3[] FoodPositions = new Vector3[20];                   // Массив координат еды.

    private GameObject[,,] Aquarium = new GameObject[150, 150, 150];     // Матрица объектов.

    public struct sSnake
    {
        public GameObject[] Snake;                                       // Объекты всех частей змейки.
        public int length;                                               // Длинна туловища в микрошагах.
        public GameObject target;                                        // Объект выбраной еды.
        public int steps;                                                // Количество шагов к еде.
        public Vector3 movePos;                                          // Позиция хода.
        public Vector3 microStep;                                        // Микрошаг.
        public bool active;                                              // Активность змейки.
    }

    public sSnake[] aSnake = new sSnake[Global.numberOfSnakes];


    // =========================== Начало начал ==========================
    private void Start()
    {
        Console.Clear();
        Console.WriteLine("<<<<<<<<< Start >>>>>>>>>");

        Restart.gameObject.SetActive(false);
        togglePause.gameObject.SetActive(true);
        snakeWinner.gameObject.SetActive(false);
        snakeNumber = Global.numberOfSnakes;
        food = Global.food;

        moveDelay = 0.1f / Global.speed;
        Time.fixedDeltaTime = moveDelay;
        sound = Global.sound;
        music = Global.music;
        if (sound || music)
        {
            toggleSaM.gameObject.SetActive(true);
            toggleSaM.isOn = true;
            GetComponent<AudioSource>().enabled = true;
        }
        else
        {
            GetComponent<AudioSource>().enabled = false;
            toggleSaM.gameObject.SetActive(false);
        }

        if (music) GetComponent<AudioSource>().Play();

        gamePaused = true;                                              // Игра на паузе.

        // Заполняем структуры.
        for (int i = 0; i < snakeNumber; i++)
        {
            aSnake[i].Snake = new GameObject[SNAKE_MAX_LEN * 10 + 1];
            aSnake[i].length = 20;
            aSnake[i].active = true;
        }

        ClearAquarium();                                                // Очистка аквариума.

        FoodCreate();                                                   // Создание еды.

        for (int i = 0; i < snakeNumber; i++) SnakeCreate(i);           // Создаем змейки.

        ScoreRefresh();                                                 // Обновление таблицы.

        if (!SearchMoveForAllSnakes())                                  // Если всем змейкам нету куда идти - 
            enabled = false;                                            // остановка скрипта.
    }
    // -------------------------------------------------------------------


    // ============= Вызивается каждые fixedDeltaTime секунд =============
    private void FixedUpdate()
    {
        if (!gamePaused) GameStep();                                      // Если не на паузе - шаг игры.
    }
    // -------------------------------------------------------------------


    // =========================== Шаг игры ==============================
    private void GameStep()
    {
        int i;
        Animation[] anim;
        AudioSource audio;

        curMicroStep = (curMicroStep + 1) % 10;                                     // Текущий микро шаг (0..10).

        for (i = 0; i < snakeNumber; i++)                                           // Передвигаем все змейки.
        {
            if (!aSnake[i].active) continue;                                        // Только для активных змеек.
            SnakeMove(i);                                                           // Передвигаем активные змейки.
        }

        if (curMicroStep == 0)                                                      // Если сделан полный шаг.
        {
            if (!SearchMoveForAllSnakes())
            {
                Console.WriteLine("<<<<<<<<< The end >>>>>>>>>");
                this.enabled = false;
            }
        }

        if (curMicroStep == 1)
        {
            for (i = 0; i < snakeNumber; i++) HeadOrientation(i);                   // Правильный поворот по ходу движения.
        }

        // Анимация съедания еды
        for (i = 0; i < snakeNumber; i++)
        {
            if (!aSnake[i].active) continue;                                        // Только для активных змеек.
            
            if (aSnake[i].steps == 1)                                               // Если ползет на еду.
            {
                anim = aSnake[i].Snake[0].GetComponents<Animation>();               // Получаем все анимации головы.
                audio = aSnake[i].Snake[0].GetComponent<AudioSource>();             // Получаем звук поедания.

                if (curMicroStep == 1) anim[0].Play("Am2");                         // В начале шага.
                if (curMicroStep == 9) anim[0].Play("Am1");                         // В конце шага.
                if (curMicroStep == 1 && sound) audio.Play();                       // В начале шага.
            }

        }

        ScoreRefresh();                                                             // Обновление таблицы.
    }
    // -------------------------------------------------------------------


    // ======================== Очистка аквариума ========================
    private void ClearAquarium()
    {
        int x, y, z;

        for (z = 0; z < 150; z++)                            // Обнуляем все позиции в пространстве куба.
            for (y = 0; y < 150; y++)
                for (x = 0; x < 150; x++)
                    Aquarium[x, y, z] = null;
    }
    // -------------------------------------------------------------------


    // =============== Случайная пустая позиция в аквариуме ==============
    private Vector3 RandomPosition()
    {
        Vector3 rPos;

        do
        {
            rPos.x = UnityEngine.Random.Range(0, 14) * 10;
            rPos.y = UnityEngine.Random.Range(0, 14) * 10;
            rPos.z = UnityEngine.Random.Range(0, 14) * 10;
        }
        while (Aquarium[(int)rPos.x, (int)rPos.y, (int)rPos.z] != null);            // Повторяем, пока не пусто.
        return rPos;
    }
    // -------------------------------------------------------------------


    // ========================= Создание еды ============================
    private void FoodCreate()
    {
        int i;
        GameObject obj;

        for (i = 0; i < food; i++)
        {
            pos = RandomPosition();                                         // Находим пустое место для еды.                     
            obj = Instantiate(foodObj, pos, Quaternion.identity);           // Создаем еду
            FoodPositions[i] = pos;                                         // Вносим еду в массив.
            Aquarium[(int)pos.x, (int)pos.y, (int)pos.z] = obj;             // Вносим еду в аквариум.
        }
    }
    // ---------------------------------------------------------------------


    // ======================== Ориентация головы ==========================
    private void HeadOrientation(int snakeNum)
    {
        Vector3 dir;
        Quaternion r;

        dir = aSnake[snakeNum].movePos - aSnake[snakeNum].Snake[0].transform.position;
        r = Quaternion.Euler(0, 0, 0);

        if (dir.x > 0) r = Quaternion.Euler(0, 90, 0);
        if (dir.x < 0) r = Quaternion.Euler(0, -90, 0);

        if (dir.y > 0) r = Quaternion.Euler(-90, 0, 0);
        if (dir.y < 0) r = Quaternion.Euler(90, 0, 0);

        if (dir.z > 0) r = Quaternion.Euler(0, 0, 0);
        if (dir.z < 0) r = Quaternion.Euler(0, 180, 0);

        aSnake[snakeNum].Snake[0].transform.rotation = r;
    }
    // ---------------------------------------------------------------------


    // =========================== Создание змеи ===========================
    private void SnakeCreate(int snakeNum)
    {
        int i;
        GameObject obj, child;
        Vector3 v, nV = new Vector3(0, 0, 0);

        switch (snakeNum)
        {
            case 0:
                pos = new Vector3(70, 20, 70);
                nV = new Vector3(0, -1, 0);
                break;
            case 1:
                pos = new Vector3(70, 120, 70);
                nV = new Vector3(0, 1, 0);
                break;
            case 2:
                pos = new Vector3(20, 70, 70);
                nV = new Vector3(-1, 0, 0);
                break;
            case 3:
                pos = new Vector3(120, 70, 70);
                nV = new Vector3(1, 0, 0);
                break;
            case 4:
                pos = new Vector3(70, 70, 20);
                nV = new Vector3(0, 0, -1);
                break;
            case 5:
                pos = new Vector3(70, 70, 120);
                nV = new Vector3(0, 0, 1);
                break;
        }

        for (i = 0; i <= aSnake[snakeNum].length; i++)
        {
            v = pos + i * nV;                                                           // Позиция следуйщей части.
            
            if (i == 0)
            {
                obj = Instantiate(snakeHead, v, Quaternion.identity);                   // Создаем голову.
                child = obj.transform.Find("skull").gameObject;
                child.GetComponent<Renderer>().material.color = col[snakeNum];          // Цвет черепа.
                child = obj.transform.Find("jaw").gameObject;
                child.GetComponent<Renderer>().material.color = col[snakeNum];          // Цвет челюсти.
            }
            else
            {
                obj = Instantiate(snakePart, v, Quaternion.identity);                   // Создаем часть.
                obj.GetComponent<Renderer>().material.color = col[snakeNum];            // Меняем цвет части.
            }
            Aquarium[(int)v.x, (int)v.y, (int)v.z] = obj;                               // Вносим голову в аквариум.
            aSnake[snakeNum].Snake[i] = obj;                                            // Записываем голову в массив.
        }
        HeadOrientation(snakeNum);
    }
    // -------------------------------------------------------------------


    // ======================= Проверка 1D-пути ==========================
    private bool CheckPath1D(Vector3 start, Vector3 end)
    {
        int i, h, j, step;
        Vector3 v, move;
        GameObject obj;

        for (h = 0; h < snakeNumber; h++)                                   // Ищем голову с позицией start.
        {
            if (!aSnake[h].active) continue;                                // Только для активных змеек.
            if (aSnake[h].Snake[0].transform.position == start) break;
        }

        move = end - start;                                                 // Перемещение
        step = Mathf.RoundToInt(move.magnitude) / 10;                       // Количество шагов.

        for (i = 1; i <= step; i++)                                         // Все позиции по маршруту.
        {
            v = start + i * 10f * move.normalized;                          // Позиция для проверки.
            if (i == 1 && h < snakeNumber)                                  // Если возможный movePos.
            {
                for (j = 0; j < h; j++)                                     // Перебираем предыдущие змейки.
                    if (aSnake[j].movePos == v) return false;               // Если туда кто-то уже ползет - путь закрыт.
            }

            obj = Aquarium[(int)v.x, (int)v.y, (int)v.z];                   // Берем объект.
            if (obj != null)                                                // Если на пути есть объект
            {
                if (obj.name != "Food(Clone)") return false;                // и это не еда - путь закрыт.
            }
        }
        return true;
    }
    // -------------------------------------------------------------------


    // ======================= Проверка 2D-пути ==========================
    private bool CheckPath2D(Vector3 start, Vector3 end, int path)
    {
        Vector3 tPos;

        tPos = end;
        switch (path)                                                                // Путь?
        {
            case (int)Direction2D.XY:
            case (int)Direction2D.YX:
                if (path == (int)Direction2D.XY) tPos.y = start.y;
                else tPos.x = start.x;                                               // Направление по Y.
                if (!CheckPath1D(start, tPos)) return false;
                if (!CheckPath1D(tPos, end)) return false;                           // Если что-то нашло - путь закрыт.
                break;
            case (int)Direction2D.XZ:
            case (int)Direction2D.ZX:
                if (path == (int)Direction2D.XZ) tPos.z = start.z;
                else tPos.x = start.x;                                               // Направление по Z.
                if (!CheckPath1D(start, tPos)) return false;
                if (!CheckPath1D(tPos, end)) return false;                           // Если что-то нашло - путь закрыт.
                break;
            case (int)Direction2D.YZ:
            case (int)Direction2D.ZY:
                if (path == (int)Direction2D.YZ) tPos.z = start.z;
                else tPos.y = start.y;                                               // Направление по Y.
                if (!CheckPath1D(start, tPos)) return false;
                if (!CheckPath1D(tPos, end)) return false;                           // Если что-то нашло - путь закрыт.
                break;
        }
        return true;
    }
    // -------------------------------------------------------------------


    // ======================= Проверка 3D-пути ==========================
    private bool CheckPath3D(Vector3 start, Vector3 end, int path)
    {
        Vector3 tPos1, tPos2;

        switch (path)                                                           // Путь?
        {
            case (int)Direction3D.XYZ:
                tPos1 = end; tPos1.y = start.y; tPos1.z = start.z;              // Первая промежуточная точка.
                tPos2 = end; tPos2.z = start.z;                                 // Вторая промежуточная точка.
                if (!CheckPath1D(start, tPos1)) return false;
                if (!CheckPath1D(tPos1, tPos2)) return false;                   // Если что-то нашло - путь закрыт.
                if (!CheckPath1D(tPos2, end)) return false;
                break;
            case (int)Direction3D.XZY:
                tPos1 = end; tPos1.z = start.z; tPos1.y = start.y;              // Первая промежуточная точка.
                tPos2 = end; tPos2.y = start.y;                                 // Вторая промежуточная точка.
                if (!CheckPath1D(start, tPos1)) return false;
                if (!CheckPath1D(tPos1, tPos2)) return false;                   // Если что-то нашло - путь закрыт.
                if (!CheckPath1D(tPos2, end)) return false;
                break;
            case (int)Direction3D.YXZ:
                tPos1 = end; tPos1.x = start.x; tPos1.z = start.z;              // Первая промежуточная точка.
                tPos2 = end; tPos2.z = start.z;                                 // Вторая промежуточная точка.
                if (!CheckPath1D(start, tPos1)) return false;
                if (!CheckPath1D(tPos1, tPos2)) return false;                   // Если что-то нашло - путь закрыт.
                if (!CheckPath1D(tPos2, end)) return false;
                break;
            case (int)Direction3D.YZX:
                tPos1 = end; tPos1.z = start.z; tPos1.x = start.x;              // Первая промежуточная точка.
                tPos2 = end; tPos2.x = start.x;                                 // Вторая промежуточная точка.
                if (!CheckPath1D(start, tPos1)) return false;
                if (!CheckPath1D(tPos1, tPos2)) return false;                   // Если что-то нашло - путь закрыт.
                if (!CheckPath1D(tPos2, end)) return false;
                break;
            case (int)Direction3D.ZXY:
                tPos1 = end; tPos1.x = start.x; tPos1.y = start.y;              // Первая промежуточная точка.
                tPos2 = end; tPos2.y = start.y;                                 // Вторая промежуточная точка.
                if (!CheckPath1D(start, tPos1)) return false;
                if (!CheckPath1D(tPos1, tPos2)) return false;                   // Если что-то нашло - путь закрыт.
                if (!CheckPath1D(tPos2, end)) return false;
                break;
            case (int)Direction3D.ZYX:
                tPos1 = end; tPos1.y = start.y; tPos1.x = start.x;              // Первая промежуточная точка.
                tPos2 = end; tPos2.x = start.x;                                 // Вторая промежуточная точка.
                if (!CheckPath1D(start, tPos1)) return false;
                if (!CheckPath1D(tPos1, tPos2)) return false;                   // Если что-то нашло - путь закрыт.
                if (!CheckPath1D(tPos2, end)) return false;
                break;
        }
        return true;
    }
    // -------------------------------------------------------------------


    // ================ Выбор последовательности направлений =============
    private void DirSelect2d(int snakeNum, Vector3 food)
    {
        float dx, dy, dz;

        dx = Math.Abs(aSnake[snakeNum].Snake[0].transform.position.x - food.x);
        dy = Math.Abs(aSnake[snakeNum].Snake[0].transform.position.y - food.y);
        dz = Math.Abs(aSnake[snakeNum].Snake[0].transform.position.z - food.z);

        if (dz == 0 && dx <= dy) { PathOrder[0] = (int)Direction2D.XY; PathOrder[1] = (int)Direction2D.YX; }
        if (dz == 0 && dy < dx) { PathOrder[0] = (int)Direction2D.YX; PathOrder[1] = (int)Direction2D.XY; }
        if (dy == 0 && dx <= dz) { PathOrder[0] = (int)Direction2D.XZ; PathOrder[1] = (int)Direction2D.ZX; }
        if (dy == 0 && dz < dx) { PathOrder[0] = (int)Direction2D.ZX; PathOrder[1] = (int)Direction2D.XZ; }
        if (dx == 0 && dy <= dz) { PathOrder[0] = (int)Direction2D.YZ; PathOrder[1] = (int)Direction2D.ZY; }
        if (dx == 0 && dz < dy) { PathOrder[0] = (int)Direction2D.ZY; PathOrder[1] = (int)Direction2D.YZ; }
    }
    // -------------------------------------------------------------------

    // ================ Выбор последовательности направлений =============
    private void DirSelect3d(int snakeNum, Vector3 food)
    {
        float dx, dy, dz;

        dx = Math.Abs(aSnake[snakeNum].Snake[0].transform.position.x - food.x);
        dy = Math.Abs(aSnake[snakeNum].Snake[0].transform.position.y - food.y);
        dz = Math.Abs(aSnake[snakeNum].Snake[0].transform.position.z - food.z);

        if (dx <= dy && dy <= dz)
        {
            PathOrder[0] = (int)Direction3D.XYZ; PathOrder[1] = (int)Direction3D.YXZ; PathOrder[2] = (int)Direction3D.XZY;
            PathOrder[3] = (int)Direction3D.ZXY; PathOrder[4] = (int)Direction3D.YZX; PathOrder[5] = (int)Direction3D.ZYX;
        }
        if (dx <= dz && dz <= dy)
        {
            PathOrder[0] = (int)Direction3D.XZY; PathOrder[1] = (int)Direction3D.ZXY; PathOrder[2] = (int)Direction3D.XYZ;
            PathOrder[3] = (int)Direction3D.YXZ; PathOrder[4] = (int)Direction3D.ZYX; PathOrder[5] = (int)Direction3D.YZX;
        }
        if (dy <= dx && dx <= dz)
        {
            PathOrder[0] = (int)Direction3D.YXZ; PathOrder[1] = (int)Direction3D.XYZ; PathOrder[2] = (int)Direction3D.YZX;
            PathOrder[3] = (int)Direction3D.ZYX; PathOrder[4] = (int)Direction3D.XZY; PathOrder[5] = (int)Direction3D.ZXY;
        }
        if (dy <= dz && dz <= dx)
        {
            PathOrder[0] = (int)Direction3D.YZX; PathOrder[1] = (int)Direction3D.ZYX; PathOrder[2] = (int)Direction3D.YXZ;
            PathOrder[3] = (int)Direction3D.XYZ; PathOrder[4] = (int)Direction3D.ZXY; PathOrder[5] = (int)Direction3D.XZY;
        }
        if (dz <= dx && dx <= dy)
        {
            PathOrder[0] = (int)Direction3D.ZXY; PathOrder[1] = (int)Direction3D.XZY; PathOrder[2] = (int)Direction3D.ZYX;
            PathOrder[3] = (int)Direction3D.YZX; PathOrder[4] = (int)Direction3D.XYZ; PathOrder[5] = (int)Direction3D.YXZ;
        }
        if (dz <= dy && dy <= dx)
        {
            PathOrder[0] = (int)Direction3D.ZYX; PathOrder[1] = (int)Direction3D.YZX; PathOrder[2] = (int)Direction3D.ZXY;
            PathOrder[3] = (int)Direction3D.XZY; PathOrder[4] = (int)Direction3D.YXZ; PathOrder[5] = (int)Direction3D.XYZ;
        }
    }
    // -------------------------------------------------------------------


    // =================== Проверка на близость к еде ====================
    private bool CheckForNearToFood(int snakeNum)
    {
        int i;

        for (i = 0; i < snakeNumber; i++)                                       // Для всех змеек.
        {
            if (!aSnake[i].active) continue;                                    // Только для активных змеек.
            if (i == snakeNum) continue;                                        // У себя не проверяет.
            if (aSnake[snakeNum].target == aSnake[i].target)                    // Если выбрали одну и ту же еду и
                if (aSnake[snakeNum].steps >= aSnake[i].steps) return false;    // этой змейке дальше - отказывается.
        }
        return true;
    }
    // -------------------------------------------------------------------


    // ================ Расчет количества шагов к еде ====================
    private int StepsCalc(int snakeNum)
    {
        Vector3 v;

        if (aSnake[snakeNum].target == null) return 100;
        v = aSnake[snakeNum].target.transform.position - aSnake[snakeNum].Snake[0].transform.position;    // Вектор от головы до еды.
        return ((int)Math.Abs(v.x) + (int)Math.Abs(v.y) + (int)Math.Abs(v.z)) / 10;                       // Возвращаем количество шагов к еде.
    }
    // -------------------------------------------------------------------


    // ======================= Поиск хода змейки =========================
    private bool SearchMove(int snakeNum)
    {
        int i, j, dimension;
        Vector3 tempV;
        double dist, distNext;
        bool swap;
        GameObject foodObj;

        // ***** Сортировка еды по расстоянию методом пузырька *****
        for (j = 1; j < food; j++)
        {
            swap = false;
            for (i = 0; i < food - j; i++)
            {
                dist = Vector3.Distance(FoodPositions[i], aSnake[snakeNum].Snake[0].transform.position);          // Растояние от головы до еды[i].
                distNext = Vector3.Distance(FoodPositions[i + 1], aSnake[snakeNum].Snake[0].transform.position);  // Растояние до следующей еды.
                if (dist > distNext)                                                          // Если следующая еда ближе - меняем вектора местами.
                {
                    pos = FoodPositions[i];
                    FoodPositions[i] = FoodPositions[i + 1];
                    FoodPositions[i + 1] = pos;
                    swap = true;
                }
            }
            if (!swap) break;
        }

        // Перебираем весь список еды.
        for (i = 0; i < food; i++)
        {
            dimension = 0;                                                                          // К-во измерений
            if (FoodPositions[i].x != aSnake[snakeNum].Snake[0].transform.position.x) dimension++;  // +1 (по X)
            if (FoodPositions[i].y != aSnake[snakeNum].Snake[0].transform.position.y) dimension++;  // +1 (по Y)
            if (FoodPositions[i].z != aSnake[snakeNum].Snake[0].transform.position.z) dimension++;  // +1 (по Z)

            pos = aSnake[snakeNum].Snake[0].transform.position;                                  // Позиция головы.
            foodObj = Aquarium[(int)FoodPositions[i].x, (int)FoodPositions[i].y, (int)FoodPositions[i].z];     // Выбраная еда.

            switch (dimension)                                                                   // Сколько направлений?
            {
                case 1:                 // **************** Одно направление ****************
                    tempV = FoodPositions[i] - pos;                                              // Вектор от головы до еды.
                    if (CheckPath1D(pos, FoodPositions[i]))                                      // Если путь свободен.
                    {
                        aSnake[snakeNum].target = foodObj;                                       // Выбраная еда.
                        aSnake[snakeNum].steps = StepsCalc(snakeNum);                            // Количество шагов к еде.
                        tempV = tempV.normalized;
                        aSnake[snakeNum].microStep = tempV;                                      // Микрошаг.
                        aSnake[snakeNum].movePos = pos + tempV * 10;                             // Куда идти.
                        if (!CheckForNearToFood(snakeNum)) break;                                // Если не успевает к еде - следущая еда.
                        return true;
                    }
                    break;
                case 2:                 // **************** Два направления ****************
                    DirSelect2d(snakeNum, FoodPositions[i]);                                         // Выбор последовательности направлений.
                    for (j = 0; j < 2; j++)                                                          // Проверяем 2 маршрута.
                    {
                        if (CheckPath2D(pos, FoodPositions[i], PathOrder[j]))                        // Если путь свободен.
                        {
                            tempV = FoodPositions[i] - pos;                                          // Вектор от головы до еды.
                            if (PathOrder[j] == (int)Direction2D.XY ||
                                PathOrder[j] == (int)Direction2D.XZ) { tempV.y = 0; tempV.z = 0; }   // Только по Х.
                            if (PathOrder[j] == (int)Direction2D.YX ||
                                PathOrder[j] == (int)Direction2D.YZ) { tempV.x = 0; tempV.z = 0; }   // Только по Y.
                            if (PathOrder[j] == (int)Direction2D.ZX ||
                                PathOrder[j] == (int)Direction2D.ZY) { tempV.x = 0; tempV.y = 0; }   // Только по Z.
                            aSnake[snakeNum].target = foodObj;                                       // Выбраная еда змейки.
                            aSnake[snakeNum].steps = StepsCalc(snakeNum);                            // Количество шагов к еде.
                            tempV = tempV.normalized;
                            aSnake[snakeNum].microStep = tempV;                                      // Микрошаг.
                            aSnake[snakeNum].movePos = pos + tempV * 10;                             // Куда идти.
                            if (CheckForNearToFood(snakeNum)) return true;                           // Если успевает к еде - Ok.
                            aSnake[snakeNum].target = null;
                            break;
                        }
                    }
                    break;

                case 3:                 // **************** Три направления ****************
                    DirSelect3d(snakeNum, FoodPositions[i]);                                         // Определение последовательности направлений. 
                    for (j = 0; j < 6; j++)                                                          // Проверяем все 6 маршрутов.
                    {
                        if (CheckPath3D(pos, FoodPositions[i], PathOrder[j]))                        // Если путь свободен.
                        {
                            tempV = FoodPositions[i] - pos;                                          // Вектор от головы до еды.
                            if (PathOrder[j] == (int)Direction3D.XYZ ||
                                PathOrder[j] == (int)Direction3D.XZY) { tempV.y = 0; tempV.z = 0; }  // Только по Х.
                            if (PathOrder[j] == (int)Direction3D.YXZ ||
                                PathOrder[j] == (int)Direction3D.YZX) { tempV.x = 0; tempV.z = 0; }  // Только по Y.
                            if (PathOrder[j] == (int)Direction3D.ZXY ||
                                PathOrder[j] == (int)Direction3D.ZYX) { tempV.x = 0; tempV.y = 0; }  // Только по Z.
                            aSnake[snakeNum].target = foodObj;                                       // Выбраная еда змейки.
                            aSnake[snakeNum].steps = StepsCalc(snakeNum);                            // Количество шагов к еде.
                            tempV = tempV.normalized;
                            aSnake[snakeNum].microStep = tempV;                                      // Микрошаг.
                            aSnake[snakeNum].movePos = pos + tempV * 10;                             // Куда идти.
                            if (CheckForNearToFood(snakeNum)) return true;                           // Если успевает к еде - Ok.
                            aSnake[snakeNum].target = null;
                            break;
                        }
                    }
                    break;
            }
        }
        return false;
    }
    // -------------------------------------------------------------------


    // ======================= Случайное движение ========================
    private bool RandomMove(int snakeNum)
    {
        int i, j;
        Vector3 rPos;

        pos = aSnake[snakeNum].Snake[0].transform.position;                         // Позиция головы.

        for (i = 0; i < 6; i++)                                                     // 6 возможных направлений
        {
            rPos = pos;
            if (i == 0)
                if (rPos.x < 140) rPos.x += 10f;                                    // Если не вышло за пределы x+10.
                else continue;                                                      // Иначе - не подходит.
            if (i == 1)
                if (rPos.x > 0) rPos.x -= 10f;                                      // Если не вышло за пределы x-10.
                else continue;                                                      // Иначе - не подходит.
            if (i == 2)
                if (rPos.y < 140) rPos.y += 10f;                                    // Если не вышло за пределы y+10.
                else continue;                                                      // Иначе - не подходит.
            if (i == 3)
                if (rPos.y > 0) rPos.y -= 10f;                                      // Если не вышло за пределы y-10.
                else continue;                                                      // Иначе - не подходит.
            if (i == 4)
                if (rPos.z < 140) rPos.z += 10f;                                    // Если не вышло за пределы z+10.
                else continue;                                                      // Иначе - не подходит.
            if (i == 5)
                if (rPos.z > 0) rPos.z -= 10f;                                      // Если не вышло за пределы z-10.
                else continue;                                                      // Иначе - не подходит.

            if (Aquarium[(int)rPos.x, (int)rPos.y, (int)rPos.z] != null) continue;  // Если занято - не подходит.

            for (j = 0; j < snakeNumber; j++)                                       // Проверка всех змеек.
            {
                if (!aSnake[j].active) continue;                                    // Только для активных змеек.
                if (j == snakeNum) continue;                                        // У себя не проверяет.
                if (aSnake[j].movePos == rPos) break;                               // Если туда ползет другая змейка - прекращаем цикл.
            }
            if (j < snakeNumber) continue;                                          // Если цикл прерван - не подходит.

            aSnake[snakeNum].movePos = rPos;                                        // Куда ползти.
            aSnake[snakeNum].microStep = (rPos - pos) / 10;                         // Микрошаг.
            aSnake[snakeNum].target = null;                                         // Цели нет.
            return true;
        }
        return false;
    }
    // -------------------------------------------------------------------


    // ======================= Движение змейки ===========================
    private void SnakeMove(int snakeNum)
    {
        int i;
        Vector3 tPos, newPos;
        GameObject obj;

        // Если еда.
        if (curMicroStep == 0 && aSnake[snakeNum].steps == 1 && aSnake[snakeNum].target != null)
        {
            pos = aSnake[snakeNum].target.transform.position;                       // Берем позицию еды.
            for (i = 0; i < food; i++)                                              // Ищем такой вектор в массиве еды.
            {
                if (FoodPositions[i] == pos)                                        // Если нашли.
                {
                    obj = aSnake[snakeNum].target;                                  // Узнаем объект еды.
                    newPos = RandomPosition();                                      // Находим новое место для этой еды.
                    obj.transform.position = newPos;                                // Перемещаем еду на сцене.
                    FoodPositions[i] = newPos;                                      // Новая позиция этой еды в списке.
                    Aquarium[(int)newPos.x, (int)newPos.y, (int)newPos.z] = obj;    // Помещаем в матрицу еду с новой пизицией.
                    Aquarium[(int)pos.x, (int)pos.y, (int)pos.z] = null;            // Освобождаем место в матрице, где была еда.
                }
            }
        }

        obj = aSnake[snakeNum].Snake[0];                                            // Берем объект головы.
        tPos = obj.transform.position;                                              // Запоминаем позицию головы.
        newPos = tPos + aSnake[snakeNum].microStep;                                 // Новая позиция головы.
        obj.transform.position = newPos;                                            // Делаем микро шаг.
        Aquarium[(int)newPos.x, (int)newPos.y, (int)newPos.z] = obj;                // Помещаем голову в новую позицию в матрице.

        if (aSnake[snakeNum].steps == 1)                                            // Если еда.
        {
            obj = Instantiate(snakePart, tPos, Quaternion.identity);                // Создаем промежуточную часть на месте головы.
            obj.GetComponent<Renderer>().material.color = col[snakeNum];            // Меняем цвет части.
            aSnake[snakeNum].length++;                                              // Удлиняем змейку.
        }
        else
        {
            obj = aSnake[snakeNum].Snake[aSnake[snakeNum].length];                  // Берем последнюю часть змейки.
            Aquarium[(int)obj.transform.position.x, (int)obj.transform.position.y,
                     (int)obj.transform.position.z] = null;                         // Освобождаем место в матрице.
            obj.transform.position = tPos;                                          // Помещаем эту часть на место головы.
        }
        Aquarium[(int)tPos.x, (int)tPos.y, (int)tPos.z] = obj;                      // Помещаем эту часть в матрицу.

        for (i = aSnake[snakeNum].length; i >= 0; i--)                              // Перебираем все части змейки.
            aSnake[snakeNum].Snake[i + 1] = aSnake[snakeNum].Snake[i];              // Сдвигаем объекты в массиве Snake[] назад.
        aSnake[snakeNum].Snake[0] = Aquarium[(int)newPos.x, (int)newPos.y, (int)newPos.z];  // Голова с новой позицией.
        aSnake[snakeNum].Snake[1] = Aquarium[(int)tPos.x, (int)tPos.y, (int)tPos.z];        // Следующая часть после головы.
    }
    // -------------------------------------------------------------------


    // ========================= Перед завершением =======================
    private void Func1()
    {
        Restart.gameObject.SetActive(true);
        togglePause.gameObject.SetActive(false);
        snakeWinner.gameObject.SetActive(false);
    }
    // -------------------------------------------------------------------


    // ==================== Поиск маршрута для всех змеек ================
    private bool SearchMoveForAllSnakes ()
    {
        int i, j;
        bool okay;
        Color color;
        float H, S, V;
        GameObject obj;

        do                                                                          // Выбор еды и поиск хода.
        {
            okay = true;
            // Поиск хода для всех змеек
            for (i = 0; i < snakeNumber; i++)                                       // Для всех змеек.
            {
                if (!aSnake[i].active) continue;                                    // Только для активных змеек.
                if (!SearchMove(i))                                                 // Если у змейки нету пути к еде
                {
                    if (!RandomMove(i))                                             // и нет случайного хода - fail.
                    {
                        aSnake[i].active = false;                                   // Змейка у которой нету пути - засыпает.
                        
                        // Делаем цвет змейки менее насыщенным
                        color = aSnake[i].Snake[1].GetComponent<Renderer>().material.color;     // Берем цвет змейки.
                        Color.RGBToHSV(color, out H, out S, out V);                 // Переводим RGB в HSV.
                        S *= 0.5f;                                                  // Уменьшаем насыщенность.
                        V *= 0.5f;                                                  // Делаем темнее.
                        color = Color.HSVToRGB(H, S, V);                            // Переводим HSV в RGB.
                        for (j = 1; j <= aSnake[i].length; j++)                     // Меняем цвет у всех частей.
                            aSnake[i].Snake[j].GetComponent<Renderer>().material.color = color;
                        // Меняем цвет головы.
                        obj = aSnake[i].Snake[0].transform.Find("skull").gameObject;           // Череп.
                        obj.GetComponent<Renderer>().material.color = color;
                        obj = aSnake[i].Snake[0].transform.Find("jaw").gameObject;             // Челюсть.
                        obj.GetComponent<Renderer>().material.color = color;


                        for (j = 0; j < snakeNumber; j++)                           // Проверка всех змеек на активность.
                            if (aSnake[j].active) break;
                        if (j == snakeNumber)                                       // Если нету активных змеек -
                        {
                            WinnerHead.GetComponent<SpriteRenderer>().sprite = SpriteHead[SnakeSortLength[0]];
                            snakeWinner.gameObject.SetActive(true);
                            Invoke("Func1", 6f);
                            return false;                                           // завершаем работу.
                        }

                    }
                    continue;                                                       // Следующая.
                }
            }

            // Проверка, чтобы у змеек не была одна цель
            for (i = 0; i < snakeNumber; i++)
            {
                if (!aSnake[i].active) continue;                                    // Только для активных змеек.
                for (j = 0; j < snakeNumber; j++)
                {
                    if (!aSnake[j].active) continue;                                // Только для активных змеек.
                    if (i == j) continue;
                    // Если у разных змеек одинаковая еда - fail.
                    if (aSnake[i].target != null && aSnake[i].target == aSnake[j].target) okay = false;
                }
            }
        }
        while (!okay);                                                              // Повторяем пока у всех змеек не будет разная цель.

        return true;
    }
    // -------------------------------------------------------------------

    
    // ###################################################################


    // ========================= Выход в меню ============================
    public void OnClose()
    {
        SceneManager.LoadScene("Start Menu");
    }
    // --------------------------------------------------------------------


    // =================== Выключение звука и музыки ======================
    public void SoundAndMusic()
    {
        if (toggleSaM.isOn)
        {
            if (Global.sound) sound = true;
            if (Global.music) music = true;
        }
        else
        {
            sound = false;
            music = false;
        }

        if (music)
        {
            GetComponent<AudioSource>().enabled = true;
            GetComponent<AudioSource>().Play();
        }
        else GetComponent<AudioSource>().enabled = false;
    }
    // --------------------------------------------------------------------


    // ======================== Переключение паузы ========================
    public void OnPause()
    {
        gamePaused = togglePause.isOn;
    }
    // --------------------------------------------------------------------


    // ======================== Обновление таблицы ========================
    private void ScoreRefresh()
    {
        int i, j, t;
        bool swap;
        GameObject obj;

        // Показываем/скрываем головы змеек.
        for (i = 0; i < 6; i++)
        {
            if (i < snakeNumber) SnakeHead[i].gameObject.SetActive(true);               // Показываем.
            else SnakeHead[i].gameObject.SetActive(false);                              // Скрываем.
        }

        for (i = 0; i < snakeNumber; i++)
            Score[i].text = (aSnake[i].length / 10 + 1).ToString();                     // Вывод набранных очков.

        // Акивные/спящие
        for (i = 0; i < snakeNumber; i++)
        {
            if (aSnake[i].active)                                                       // Если змейка активна.
            {
                SnakeHead[i].GetComponent<SpriteRenderer>().sprite = SpriteHead[i];     // Спрайт активной змейки.
                Score[i].color = new Color(1f, 1f, 1f, 1f);                             // Цвет цифр очков белый.
            }
            else                                                                        // Если змейка не активна.
            {
                SnakeHead[i].GetComponent<SpriteRenderer>().sprite = SpriteHeadSleep[i];// Спрайт не активной змейки.
                Score[i].color = new Color(0.8f, 0.8f, 0.8f, 1f);                       // Цвет цифр очков сероватый.
            }
        }

        // ***** Сортировка змеек по длине *****
        for (i = 0; i < 6; i++) SnakeSortLength[i] = i;                     
        for (j = 1; j < snakeNumber; j++)
        {
            swap = false;
            for (i = 0; i < snakeNumber - j; i++)
            {
                if (aSnake[SnakeSortLength[i]].length < aSnake[SnakeSortLength[i + 1]].length)
                {
                    // swap
                    t = SnakeSortLength[i];
                    SnakeSortLength[i] = SnakeSortLength[i + 1];
                    SnakeSortLength[i + 1] = t;
                    swap = true;
                }
            }
            if (!swap) break;
        }

        // ***** Перемещаем позиции змеек в таблице *****
        for (i = 0; i < snakeNumber; i++)                                   // От самой длинной до самой короткой.
        {
            obj = SnakeHead[SnakeSortLength[i]];                            // Берем следуйщую змейку.
            pos = new Vector3(-540f, 150f - i * 60, 0);
            obj.transform.localPosition = pos;                              // Меняем позицию змейки в зависивости от её места.
        }
    }
    // --------------------------------------------------------------------


    // ============================ Рестарт игры ==========================
    public void GameRestart ()
    {
        SceneManager.LoadScene("Main");
    }
    // --------------------------------------------------------------------

    /*
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~ Вывод всего ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        private void OutAll(int snakeNum)
        {
            int i;

            Console.WriteLine("snakeNum " + snakeNum);
            Console.WriteLine("curMicroStep " + curMicroStep);
            Console.Write("struct.Snake[] ");
            for (i = 0; i < aSnake[snakeNum].length + 1; i++) Console.Write(aSnake[snakeNum].Snake[i] + " ");
            Console.WriteLine("");

            Console.WriteLine("");
            Console.WriteLine("struct.length " + aSnake[snakeNum].length);
            Console.WriteLine("struct.target " + aSnake[snakeNum].target);
            Console.WriteLine("struct.steps " + aSnake[snakeNum].steps);
            Console.WriteLine("struct.movePos " + aSnake[snakeNum].movePos);
            Console.WriteLine("struct.microStep " + aSnake[snakeNum].microStep);
            Console.Write("FoodPositions[] ");
            for (i = 0; i < food; i++) Console.Write(FoodPositions[i] + " ");
            Console.WriteLine("");
            Console.WriteLine("----------------------------------");
        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
*/

    // ~~~~~~~~~~~~~~~~~~~~~~~~~ Вывод объекта ~~~~~~~~~~~~~~~~~~~~~~~~~~~
    private void OutObj (GameObject obj)
    {
        if (obj != null) Console.WriteLine("<" + obj.name + ">");
        else Console.WriteLine("<null>");
    }
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    
    // ~~~~~~~~~~~~~~~~~~ Проверка на столкновение ~~~~~~~~~~~~~~~~~~~~~~~
    private void CheckForCollision(int snakeNum)
    {
        int i, j=0;
        Vector3 v;
        GameObject obj;

        v = aSnake[snakeNum].movePos;                                           // Куда ползет
        for (i = 0; i < snakeNum; i++)                                          // Все змейки
        {
            for (j = 0; j <= aSnake[i].length; j++)                             // Все части змейки
            {
                if (aSnake[i].Snake[j].transform.position == v) break;          // Если там есть часть змейки - прерываем цикл j
            }
            if (j < aSnake[i].length + 1) break;                                // Если цикл j прерван - прерываем цикл i
        }
        if (i == snakeNum) return;                                              // Если цикл i завершился нормально - выход

        // Найдено совпадение !
        Console.WriteLine("Alarm !!!!!!!");
        Console.WriteLine("Snake[" + snakeNum + "] ---> " + v);
        Console.WriteLine("Но там часть " + j + " Snake[" + i + "]");
        Console.Write("В Aquarium[] там ");
        obj = Aquarium[Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z)];
        OutObj(obj);
        Console.WriteLine("--------------");
        StopCoroutine("GameStep");                                              // Прекращаем
    }
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


    // ~~~~~~~~~~~~~~~~~~~~ Проверка на movePos ~~~~~~~~~~~~~~~~~~~~~~~~~~
    private void CheckMovePos(int snakeNum)
    {
        Vector3 v;
        GameObject obj;

        v = aSnake[snakeNum].movePos;                                                           // Куда ползет?
        Console.Write("Snake[" + snakeNum + "] ---> " + v);

        obj = Aquarium[Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z)];    // Что там в аквариуме?
        if (obj != null) Console.Write(" !!! ");
        OutObj(obj);

        //Console.WriteLine("--------------");
        enabled = false;
        StopCoroutine("GameStep");                                              // Прекращаем
    }
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


    
}






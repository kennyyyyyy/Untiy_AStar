using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// closeList�б���Բ���Ҫ
/// </summary>
public class AStar : MonoBehaviour
{
    [Header("GameObject")]
    public GameObject blockPrefab;
    public Toggle setStartToggle, setEndToggle, 
        setObstacleToggle, delSetToggle;

    [Header("����")]
    public int height, width;

    [Header("֡��")]
    public int fps;

    private Point startPoint, EndPoint;         //�����յ�
    private List<Point> openList, closeList;    //�б���ǰ��Ҫ���ʵĺ��Ѿ����ʹ���
    private Point[,] map;                       //��¼��ͼ�����Ϣ
    private bool setStart, setEnd;              //�Ƿ����ù������յ�
    private bool isStart = false, isEnd = false, isColored = false; //��ʼ������������������ʼ��ɫ


    void Start()
    {
        Init();
    }

    /// <summary>
    /// ��ʼ���������������ɵ�ͼ��
    /// </summary>
    private void Init()
    {

        openList = new List<Point>();
        closeList = new List<Point>();
        map = new Point[height * 2 + 1, width * 2 + 1];

        setStart = false;
        setEnd = false;
        isStart = false;
        isEnd = false;
        isColored = false;

        for (int i = -height; i < height; i++)
        {
            for(int j = -width; j < width; j++)
            {
                map[i + height, j + width] = new Point(i + height, j + width);
                map[i + height, j + width].spriteRenderer = 
                    Instantiate(blockPrefab, new Vector2(i, j), Quaternion.identity, transform).GetComponent<SpriteRenderer>();
            }
        }
    }

    //��ʼ���ң�����ʼ�ڵ������openlist�б�
    public void StartFind()
    {
        if (setStart && setEnd)
        {
            openList.Add(startPoint);
            startPoint.G = 0;
            CalculateH(startPoint, EndPoint);
            isStart = true;
        }
    }

    //���õ�ͼ�����Ϣ����ز���
    public void ResetMap()
    {
        openList.Clear();
        closeList.Clear(); 

        setStart = false;
        setEnd = false;
        isStart = false;
        isEnd = false;
        isColored = false;

        for (int i = -height; i < height; i++)
        {
            for (int j = -width; j < width; j++)
            {
                map[i + height, j + width].parent = null;
                map[i + height, j + width].Type = BlockType.NULL;

            }
        }
    }

    //����Ԥ�ƾ���
    private void CalculateH(Point start, Point end)
    {
        start.H = Mathf.Abs(start.x - end.x) + Mathf.Abs(start.y - end.y);
    }

    void Update()
    {
        Application.targetFrameRate = fps;

        //��ɫ
        if (Input.GetMouseButton(0) && !isStart)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit))
            {
                if(hit.collider.CompareTag("Block"))
                {
                    Vector3 pos = hit.collider.transform.position;
                    int posX = (int)(pos.x + height);
                    int posY = (int)(pos.y + width);
                    Point cur = map[posX, posY];
                    if (delSetToggle.isOn)
                    {
                        if (cur.Type == BlockType.Start)
                            setStart = false;
                        if (cur.Type == BlockType.End)
                            setEnd = false;
                        cur.isObstacle = false;
                        cur.Type = BlockType.NULL;
                    }
                    else if (cur.Type == BlockType.NULL)
                    {
                        if (setStartToggle.isOn && !setStart)
                        {
                            cur.Type = BlockType.Start;
                            startPoint = cur;
                            setStart = true;
                        }
                        else if (setEndToggle.isOn && !setEnd)
                        {
                            cur.Type = BlockType.End;
                            EndPoint = cur;
                            setEnd = true;
                        }
                        else if (setObstacleToggle.isOn)
                        {
                            cur.Type = BlockType.Obstacle;
                            cur.isObstacle = true;
                        }
                    }

                }
            }
        }
        if (isStart)
        {
            if(!isEnd)
                FindPath();
            else if(!isColored)
            {
                isColored = true;
                StartCoroutine(ColoredPath());
            }

        }
    }


    /// <summary>
    /// ����·��
    /// </summary>
    private void FindPath()
    {
        if(openList.Count == 0)
        {
            return;
        }

        //����FֵС����ǰ
        openList.Sort();

        int minF = openList[0].F, idx = 0;
        //��F��С�ļ������ȫ��������֮���ٿ�ʼ��һ��
        while(idx < openList.Count && minF == openList[idx].F)
        {
            Point curPoint = openList[idx++];

            Point nextPoint;
            List<Vector2> dir = new List<Vector2> { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
            //�ĸ��������β���
            for (int i = 0; i < 4; i++)
            {
                //Խ���ж�
                if (curPoint.x + dir[i].x >= 0 && curPoint.x + dir[i].x < height * 2 && curPoint.y + dir[i].y >= 0 && curPoint.y + dir[i].y < width * 2)
                {
                    nextPoint = map[(int)(curPoint.x + dir[i].x), (int)(curPoint.y + dir[i].y)];
                    //�����ϰ���Ż��nextPoint���д���
                    if (nextPoint.Type != BlockType.Obstacle)
                    {
                        //����closeList�б��У������ǹر�״̬
                        if (!closeList.Contains(nextPoint))
                        {
                            nextPoint.SetParent(curPoint);

                            nextPoint.G = curPoint.G + 1;
                            CalculateH(nextPoint, EndPoint);
                            if (!openList.Contains(nextPoint))
                            {
                                nextPoint.Type = BlockType.Opened;
                                openList.Add(nextPoint);
                            }
                        }
                        else
                        {
                            //�ǹر�״̬���������costΪ��С��
                            if (curPoint.G + 1 < nextPoint.G)
                            {
                                nextPoint.SetParent(curPoint);
                                nextPoint.G = curPoint.G + 1;
                                CalculateH(nextPoint, EndPoint);
                                //if (!openList.Contains(nextPoint))
                                //{
                                //    nextPoint.Type = BlockType.Opened;
                                //    openList.Add(nextPoint);
                                //}
                            }
                        }
                    }
                }
            }
            //�Ƴ�openList������closeList������ǰ��ر�
            closeList.Add(curPoint);
            openList.Remove(curPoint);
            curPoint.Type = BlockType.Closed;

            //openList����End�ڵ㣬��ʾ�Ѿ��ҵ���·��
            if (openList.Contains(EndPoint))
            {
                isEnd = true;
            }
        }

       
    }

    /// <summary>
    /// ���·��
    /// </summary>
    IEnumerator ColoredPath()
    {
        while(true)
        {
            if(EndPoint.G == 0)
            {
                break;
            }
            EndPoint.Type = BlockType.Final;
            EndPoint = EndPoint.parent;
            yield return null;
        }
        yield return null;
    }

}

/// <summary>
/// ��
/// </summary>
[System.Serializable]
public class Point:IComparable<Point>
{
    public int x, y;
    public Point parent;
    public bool isObstacle = false;
    public int G, H;
    public int F 
    { 
        get
        {
            //text.text = (G + H).ToString();
            return G + H;
        }
    }
    public SpriteRenderer spriteRenderer;
    public Text text;

    //��ĵ�ǰ״̬������ʱ�ı���ɫ
    private BlockType type;
    public BlockType Type
    {
        get => type;
        set
        {
            if (value != BlockType.NULL && (type == BlockType.Start || type == BlockType.End))
                return;
            type = value;
            switch (type)
            {
                case BlockType.NULL:
                    spriteRenderer.color = Color.white;
                    break;
                case BlockType.Obstacle:
                    spriteRenderer.color = Color.black;
                    break;
                case BlockType.Start:
                    spriteRenderer.color = Color.yellow;
                    break;
                case BlockType.End:
                    spriteRenderer.color = Color.blue;
                    break;
                case BlockType.Opened:
                    spriteRenderer.color = Color.red;
                    break;
                case BlockType.Closed:
                    spriteRenderer.color = Color.gray;
                    break;
                case BlockType.Final:
                    spriteRenderer.color = Color.cyan;
                    break;
                default:
                    break;
            }
        }
    }


    public Point() : this(0, 0)
    {

    }

    public Point(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    /// <summary>
    /// ���ø��׵㣬����·������
    /// </summary>
    /// <param name="parent"></param>
    public void SetParent(Point parent)
    {
        this.parent = parent;
    }

    //����Sort()
    public int CompareTo(Point other)
    {
        if (this.F == other.F)
            return 0;
        else if (F < other.F)
            return -1;
        else
            return 1;
    }

}

//���״̬
public enum BlockType
{
    NULL,
    Obstacle,
    Start,
    End,
    Opened,
    Closed,
    Final
}
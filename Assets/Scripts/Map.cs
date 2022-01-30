using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    static public Map m_instance;

    public TextAsset m_levelData;

    public GameObject m_pixelPrefab;
    public GameObject m_targetTilePrefab;
    public GameObject m_wallPrefab;
    public GameObject m_blockPrefab;
    public GameObject m_iceBlockPrefab;
    public GameObject m_fireBlockPrefab;
    public GameObject m_balanceBlockPrefab;
    public GameObject m_crystalPrefab;
    public GameObject m_flamePrefab;
    public GameObject m_successPrefab;
    
    public int m_levelNum = 2;

    public int m_x = 16;
    public int m_y = 8;

    int[,] m_blocks;
    int[,] m_walls;
    int[,] m_floor;


    enum BlockType
    {
        Floor,
        Fire,
        Ice,
        Balance,
    };


    class BlockData
    {
        public GameObject m_block;
        public int x,y;// block position
        public bool moving;
        public bool m_removeAfterMove;
        public bool m_canSlide;
        public BlockType m_type;
        public int m_lastDX, m_lastDY;
        public int m_steps;

        public void SetMoving(bool m, int tx, int ty)
        {
            moving = m;
            x = tx;
            y = ty;
        }

        public void AddStep()
        {
            m_steps++;
        }

        public void SetCanSlide(bool s)
        {
            m_canSlide = s;
        }
        
        public void SetPosition(float x, float y)
        {
            m_block.transform.position = new Vector3(x, 0.5f, y);
        }

        public void SetType(BlockType bt)
        {
            m_type = bt;
        }

        public void SetRemoveAfterMove()
        {
            m_removeAfterMove = true;
        }

        public void SetLastDirection(int x, int y)
        {
            m_lastDX = x;
            m_lastDY = y;
        }

        public void UpdateObject()
        {
            GameObject go;
            switch (m_type)
            {
                case BlockType.Fire:
                    if(m_block.tag == "Fire")return;
                    go = Instantiate(Map.m_instance.m_fireBlockPrefab);
                    break;
                case BlockType.Ice:
                    if(m_block.tag == "Ice")return;
                    go = Instantiate(Map.m_instance.m_iceBlockPrefab);
                    break;
                case BlockType.Balance:
                default:
                    go = Instantiate(Map.m_instance.m_balanceBlockPrefab);
                    break;
            }
            go.transform.position = m_block.transform.position;
            Destroy(m_block);
            m_block = go;
        }
    };

    List<BlockData> m_blockData = new List<BlockData>();
    List<GameObject> m_floorTiles = new List<GameObject>();
    List<GameObject> m_wallList = new List<GameObject>();

    void Awake()
    {
        m_instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        ResetLevel();
    }

    public void StartNextLevel()
    {
        m_levelNum++;
        ResetLevel();
    }

    void LoadLevel(int num)
    {
        var content = m_levelData.text;
        var data = content.Split(",");

        int numLevels = int.Parse(data[0]);

        if(num>numLevels)
        {
            Debug.Log("OUT OF LEVEL DATA");
        }

        int levelBlobSize = 16*8*3;
        int offsetToFloor = 16*8;
        int offsetToBlocks = 16*8*2;

        int index = 1 + ((num-1) * levelBlobSize);

        for (int y = 0; y < m_y; ++y)
        {
            for (int x = 0; x < m_x; ++x)
            {
                m_walls[y, x] = int.Parse(data[index]);
                m_floor[y, x] = int.Parse(data[index+offsetToFloor]);
                m_blocks[y, x] = int.Parse(data[index+offsetToBlocks]);
                index++;
            }
        }
        
    }

    public void ResetLevel()
    {
        // delete all blocks
        foreach(var b in m_blockData)
        {
            Destroy(b.m_block);
        }
        m_blockData.Clear();

        foreach(var b in m_floorTiles)
        {
            Destroy(b);
        }
        m_floorTiles.Clear();

        foreach(var b in m_wallList)
        {
            Destroy(b);
        }
        m_wallList.Clear();

        m_walls = new int[m_y, m_x];
        m_blocks = new int[m_y, m_x];
        m_floor = new int[m_y, m_x];

        LoadLevel(m_levelNum);



        AddWalls();
        AddBlocks();
        CreatePixels();
    
        GameManager.m_instance.FadeToClear();

        if(m_levelNum>1)
        {
            GameManager.m_instance.ShowLevelText(m_levelNum);
        }
    }

    public bool IsWall(int x, int y)
    {
        return(m_walls[7-y,x]>=1);
    }
    public bool IsBlock(int x, int y)
    {
        return(m_blocks[7-y,x]>=1);
    }
    public bool IsIce(int x, int y)
    {
        return(m_blocks[7-y,x] == (int)BlockType.Ice);
    }

    public bool CanMoveBlock(int x, int y, int dx, int dy, int stepsTaken)
    {
        bool movable = true;

        // this would stop balance blocks moving
        if(m_blocks[7-(y),(x)]==(int)BlockType.Balance)movable = false;

        if(m_walls[7-(y+dy),(x+dx)]>0)movable = false;
        // can combine blocks! 
        if(stepsTaken > 0 && m_blocks[7-(y+dy),(x+dx)]>0)movable = false;
       
        return movable;
    }
    public void MoveBlock(int x, int y, int dx, int dy)
    {
        // icey blocks should slide!!!!

        BlockType prevBT = (BlockType)m_blocks[7-(y+dy),(x+dx)];
        BlockType currBT = (BlockType)m_blocks[7-(y),(x)];
        BlockType finalBT = BlockType.Floor;
        m_blocks[7-(y),(x)]=0;
        if(prevBT == BlockType.Floor)finalBT=currBT;
        
        if(prevBT == currBT)finalBT = (currBT==BlockType.Fire?BlockType.Ice:BlockType.Fire);
        if(prevBT == BlockType.Fire && currBT == BlockType.Ice)finalBT=BlockType.Balance;
        if(prevBT == BlockType.Ice && currBT == BlockType.Fire)finalBT=BlockType.Balance;
        m_blocks[7-(y+dy),(x+dx)] = (int)finalBT;
        
        // mark any block that will be merged for removal
        foreach(var b in m_blockData)
        {
            if(b.x == x+dx && b.y ==y+dy)
            {
                b.SetRemoveAfterMove();
            }
        }
        
        // find block in list and move it
        foreach(var b in m_blockData)
        {
            if (b.x == x && b.y == y)
            {
                if (currBT == BlockType.Ice && finalBT == BlockType.Ice)
                {
                    b.SetCanSlide(true);
                }
                else
                {

                    if (currBT == BlockType.Fire && finalBT == BlockType.Fire)
                    {
                        b.SetCanSlide(true);
                    }
                    else
                    {

                        b.SetCanSlide(false);
                    }
                }
                b.SetLastDirection(dx, dy);
                b.SetMoving(true, x + dx, y + dy);
                b.SetType((BlockType)m_blocks[7 - (y + dy), (x + dx)]);
            }
        }
    }

    void AddWalls()
    {
        for (int y = 0; y < 8; ++y)
        {
            for (int x = 0; x < 16; ++x)
            {
                if(m_walls[7-y,x]==1)
                {
                GameObject go = Instantiate(m_wallPrefab, transform);
                //go.transform.parent = transform;
                go.transform.position = new Vector3(x, 0.0f, y);
            
                //byte c = 96;//(byte)(((x+y)&1) * 255);
                byte c = (byte)Random.Range(16, 64);

                m_wallList.Add(go);

                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                mr.material.SetColor("_BaseColor", new Color32((byte)(c+8), c, c, 255));
                }
            }
        }
    }

    void AddBlocks()
    {
        for (int y = 0; y < 8; ++y)
        {
            for (int x = 0; x < 16; ++x)
            {
                if (m_blocks[7 - y, x] > 0)
                {
                    BlockType blockType = (BlockType)(m_blocks[7 - y, x]);

                    GameObject go;
                    if(blockType == BlockType.Ice)
                    {
                        go = Instantiate(m_iceBlockPrefab, transform);
                    }
                    else
                    if(blockType == BlockType.Fire)
                    {
                        go = Instantiate(m_fireBlockPrefab, transform);
                    }
                    else
                    {
                        go = Instantiate(m_blockPrefab, transform);
                    }
                    BlockData block = new BlockData();
                    block.m_block = go;
                    block.x = x;
                    block.y = y;
                    block.moving = false;
                    block.m_type = blockType;

                    m_blockData.Add(block);

                    //go.transform.parent = transform;
                    go.transform.position = new Vector3(x, 0.5f, y);

                    SetBlockColor(block.m_block, block.m_type);
                    
                }
            }
        }
    }

    void SetBlockColor(GameObject go, BlockType bt)
    {
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        switch(bt)
        {
            case BlockType.Fire:
                mr.material.SetColor("_BaseColor", new Color32(255, 64, 64, 255));
                break;
            case BlockType.Ice:
                mr.material.SetColor("_BaseColor", new Color32(128, 128, 255, 255));
                break;
            case BlockType.Balance:
                mr.material.SetColor("_BaseColor", new Color32(128, 255, 128, 255));
                break;
            default:
                mr.material.SetColor("_BaseColor", new Color32(200, 200, 200, 255));
                break;
        }
    }

    void CreatePixels()
    {
        for (int y = 0; y < m_y; ++y)
        {
            for (int x = 0; x < m_x; ++x)
            {
                if(m_floor[7-y,x]==0)continue;

                GameObject go;
                
                if(m_floor[7-y,x]==2)
                    go = Instantiate(m_targetTilePrefab, transform);
                else
                {
                    go = Instantiate(m_pixelPrefab, transform);
                    if(m_floor[7-y,x]==3)
                    {
                        Player.m_instance.SetPosition(x,y);
                    }
                }
                go.transform.position = new Vector3(x, 0.0f, y);
            
                m_floorTiles.Add(go);

                //byte c = (byte)Random.Range(48, 64);

                //MeshRenderer mr = go.GetComponent<MeshRenderer>();
                //mr.material.SetColor("_BaseColor", new Color32(c, c, c, 255));
            }
        }

        // add some crystals around the top
        for (int x = 0; x < m_x; x++)
        {
            GameObject go = Instantiate(m_crystalPrefab, transform);
            go.transform.position = new Vector3(x, 0.5f, m_y);//-0.5f);
            float s = Random.Range(20.0f, 50.0f);
            go.transform.localScale = new Vector3(s,s,s);
            go.transform.Rotate(0.0f, Random.Range(180.0f-45.0f, 180.0f+45.0f), 0.0f); 

            m_floorTiles.Add(go);
        }
        // and a bit down the edges
        float sy = 15.0f;
        for (int y = m_y-4; y < m_y; y++)
        {
           
            GameObject go = Instantiate(m_crystalPrefab, transform);
            go.transform.position = new Vector3(0.0f, 0.5f, y);
            float s = sy;//Random.Range(5.0f, 30.0f);
            go.transform.localScale = new Vector3(s,s,s);
            go.transform.Rotate(0.0f, Random.Range(180.0f-45.0f, 180.0f+45.0f), 0.0f); 

            m_floorTiles.Add(go);

            go = Instantiate(m_crystalPrefab, transform);
            go.transform.position = new Vector3(m_x-1.0f, 0.5f, y);
            s = sy;//Random.Range(5.0f, 30.0f);
            go.transform.localScale = new Vector3(s,s,s);
            go.transform.Rotate(0.0f, Random.Range(180.0f-45.0f, 180.0f+45.0f), 0.0f); 

            sy += 7.0f;
            m_floorTiles.Add(go);
        }

         // add some flames across the top
        for (int x = 1; x < m_x; x++)
        {
            GameObject go = Instantiate(m_flamePrefab, transform);
            go.transform.position = new Vector3(x, 1.0f, m_y+2.0f);
            float s = Random.Range(3.0f, 4.0f);
            go.transform.localScale = new Vector3(s,s,s);
            //go.transform.Rotate(0.0f, Random.Range(180.0f-45.0f, 180.0f+45.0f), 0.0f); 

            m_floorTiles.Add(go);
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool runRemovePhase = false;

        foreach(var b in m_blockData)
        {
            if(b.moving)
            {
                float px = b.m_block.transform.position.x;
                float py = b.m_block.transform.position.z;

                float speed = 10.0f * Time.deltaTime;
                if(b.x > b.m_block.transform.position.x)
                {
                    px += speed;
                    if(px > b.x)
                    {
                        bool stop = true;
                        if(b.m_canSlide)
                        {
                            if(CanMoveBlock(b.x,b.y,b.m_lastDX,b.m_lastDY,b.m_steps))
                            {
                                MoveBlock(b.x,b.y,b.m_lastDX,b.m_lastDY);
                                b.AddStep();
                                stop = false;
                            }
                        }
                        if(stop)
                        {
                            px = b.x; 
                            b.moving = false;
                            b.UpdateObject();
                            //SetBlockColor(b.m_block, b.m_type);
                            runRemovePhase = true;
                        }
                    }
                }
                if(b.x < b.m_block.transform.position.x)
                {
                    px -= speed;
                    if(px < b.x)
                    {
                        bool stop = true;
                        if(b.m_canSlide)
                        {
                            if(CanMoveBlock(b.x,b.y,b.m_lastDX,b.m_lastDY,b.m_steps))
                            {
                                MoveBlock(b.x,b.y,b.m_lastDX,b.m_lastDY);
                                b.AddStep();
                                stop = false;
                            }
                        }
                        if(stop)
                        {
                            px = b.x; 
                            b.moving = false;
                            b.UpdateObject();
                            //SetBlockColor(b.m_block, b.m_type);
                            runRemovePhase = true;
                        }
                    }
                }

                if(b.y > b.m_block.transform.position.z)
                {
                    py += speed;
                    if(py > b.y)
                    {
                        bool stop = true;
                        if(b.m_canSlide)
                        {
                            b.AddStep(); 
                            if(CanMoveBlock(b.x,b.y,b.m_lastDX,b.m_lastDY,b.m_steps))
                            {
                                MoveBlock(b.x,b.y,b.m_lastDX,b.m_lastDY);
                                stop = false;
                            }
                        }
                        if(stop)
                        {
                            py = b.y; 
                            b.moving = false;
                            b.UpdateObject();
                            //SetBlockColor(b.m_block, b.m_type);
                            runRemovePhase = true;
                        }
                    }
                }
                if(b.y < b.m_block.transform.position.z)
                {
                    py -= speed;
                    if(py < b.y)
                    {
                        bool stop = true;
                        if(b.m_canSlide)
                        {
                            if(CanMoveBlock(b.x,b.y,b.m_lastDX,b.m_lastDY,b.m_steps))
                            {
                                MoveBlock(b.x,b.y,b.m_lastDX,b.m_lastDY);
                                b.AddStep();
                                stop = false;
                            }
                        }
                        if(stop)
                        {
                            py = b.y; 
                            b.moving = false;
                            b.UpdateObject();
                            //SetBlockColor(b.m_block, b.m_type);
                            runRemovePhase = true;
                        }
                    }
                }

                b.SetPosition(px,py);

            }
        }

        if(runRemovePhase)
        {
            BlockData bdToRemove = null;
            foreach(var b in m_blockData)
            {
                if(b.m_removeAfterMove)
                {
                    Destroy(b.m_block);
                    bdToRemove = b;
                }
            }

            m_blockData.Remove(bdToRemove);

            TestForLevelSuccess();
        }
    }

    void AddSuccessFX()
    {
        foreach(var b in m_blockData)
        {
            if (b.m_type == BlockType.Balance)
            {
                GameObject go = Instantiate(m_successPrefab, b.m_block.transform);
            }
        }
    }

    void TestForLevelSuccess()
    {
        if(m_levelNum >=5)return;

        // count targets in level
        int tC = 0;
        for (int y = 0; y < m_y; ++y)
        {
            for (int x = 0; x < m_x; ++x)
            {
                if(m_floor[y,x]==2)tC++;
            }
        }

        int placed =0;
        bool failed = false;
        foreach(var b in m_blockData)
        {
            if (b.m_type == BlockType.Balance)
            {
                if(m_floor[7-b.y,b.x]==2)
                    placed++;
                else
                    failed = true;
            }
        }

        if(failed)
        {
             GameManager.m_instance.FailLevel();
            Player.m_instance.Defeat();
        }

        if(placed == tC)
        {
            GameManager.m_instance.NextLevel();
            AddSuccessFX();
            Player.m_instance.Win();
        }

    }
}

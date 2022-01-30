using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    static public Player m_instance;

    public Camera m_cam;

    public Animator m_animator;

    public float m_camHeight = 50.0f;
    public float m_camDrop = 50.0f;
    public float m_camSpeed = 5.0f;
    public float m_speed = 10.0f;
    
    int m_x;
    int m_y;

    float m_autoRepeatDelay = 0.0f;
    float m_autoRepeatTime = 0.25f;

    Vector3 m_pos;
    public float m_moveLerp = 0.1f;

    bool m_isActive;

    void Awake()
    {
        m_instance = this;
    }


    // Start is called before the first frame update
    void Start()
    {
    }

    public void SetPosition(int x, int y)
    {
        m_x = x;
        m_y = y;
        transform.position = new Vector3(m_x, 0.0f, m_y);
        m_pos = transform.position;

        m_isActive = true;
    }

    void UpdateCamera()
    {
        Vector3 targetPos = new Vector3(m_x, m_camHeight, m_y-m_camDrop);
        Vector3 newPos = Vector3.Lerp(m_cam.transform.position, targetPos, m_camSpeed * Time.deltaTime);

        m_cam.transform.position = newPos;
      //  m_cam.transform.LookAt(transform.position);
    }

    bool TryMove(int x, int y)
    {
        int tx = m_x + x;
        int ty = m_y + y;

        m_autoRepeatDelay = m_autoRepeatTime;

        if(Map.m_instance.IsWall(tx,ty))return false;

        if(Map.m_instance.IsBlock(tx,ty))
        {
            if(Map.m_instance.CanMoveBlock(tx,ty,x,y, 0)==false)return false;
            
            Map.m_instance.MoveBlock(tx,ty,x,y);
        }


        m_x += x;
        m_y += y;

        return true;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCamera();

        bool mup = false;
        bool mdown = false;
        bool mleft = false;
        bool mright = false;

        if (Input.GetKey("escape"))
        {
            Application.Quit();
        }

        if(m_isActive)
        {
            if (Input.GetKeyDown("up"))mup = true;
            if (Input.GetKeyDown("down"))mdown = true;
            if (Input.GetKeyDown("left"))mleft = true;
            if (Input.GetKeyDown("right"))mright = true;
        }
          float dx = m_pos.x - m_x;
            float dy = m_pos.z - m_y;
            float dist = Mathf.Sqrt((dx*dx)+(dy*dy));
            bool close = dist < 0.25f;
     
        if(m_autoRepeatDelay>0.0f)
        {
            m_autoRepeatDelay -= Time.deltaTime;
            
            if(m_autoRepeatDelay <= 0.0f && close)
            {
                if(m_isActive)
                {
                    if (Input.GetKey("up")) mup = true;
                    if (Input.GetKey("down")) mdown = true;
                    if (Input.GetKey("left")) mleft = true;
                    if (Input.GetKey("right")) mright = true;
                }
            }
        }

      

        if(mup)transform.rotation = Quaternion.Euler(0, 0, 0);
        if(mright)transform.rotation = Quaternion.Euler(0, 90, 0);
        if(mdown)transform.rotation = Quaternion.Euler(0, 180, 0);
        if(mleft)transform.rotation = Quaternion.Euler(0, -90, 0);


        if (mup)TryMove(0,1);
        if (mdown)TryMove(0,-1);
        if (mleft)TryMove(-1,0);
        if (mright)TryMove(1,0);

        if (Input.GetKeyDown("r"))Map.m_instance.ResetLevel();


        float speed = m_speed * Time.deltaTime;

        float posX = m_pos.x;
        float posY = m_pos.z;
        
        if(m_x>posX)
        {
            posX += speed;
            if(posX > m_x)
            {
                posX = m_x;
            };
        }
        if(m_x<posX)
        {
            posX -= speed;
            if(posX < m_x)
            {
                posX = m_x;
            }
        }

        if(m_y>posY)
        {
            posY += speed;
            if(posY > m_y)
            {
                posY = m_y;
            }
        }
        if(m_y<posY)
        {
            posY -= speed;
            if(posY < m_y)
            {
                posY = m_y;
            }
        }

        m_pos = new Vector3(posX, 0.0f, posY);

        float step = m_moveLerp * Time.deltaTime;
        if(m_pos.x > transform.position.x+step )transform.position += Vector3.right * step;
        if(m_pos.x < transform.position.x-step )transform.position -= Vector3.right * step;

        if(m_pos.z > transform.position.z+step )transform.position += Vector3.forward * step;
        if(m_pos.z < transform.position.z-step )transform.position -= Vector3.forward * step;


        bool walking = false;
        if(m_isActive)
        {
            if (Input.GetKey("up"))walking = true;
            if (Input.GetKey("down"))walking = true;
            if (Input.GetKey("left"))walking = true;
            if (Input.GetKey("right"))walking = true;
        }
        
        if (walking)
        {
            m_animator.SetBool("walking", true);
        }
        else
        {
            m_animator.SetBool("walking", false);
        }

        //transform.position = Vector3.Lerp(transform.position, m_pos, m_moveLerp * Time.deltaTime);
    }

    public void Win()
    {
        m_animator.SetTrigger("win");
        m_isActive = false;
    }

    public void Defeat()
    {
        m_animator.SetTrigger("defeat");
        m_isActive = false;
    }
}

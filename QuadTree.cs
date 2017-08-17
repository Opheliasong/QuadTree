using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Map 구조를 저장하는 QuadTree
/// 내부 node의 데이터는 short이며, 
/// 이 short의 value는 각 맵의 타일의 타입과 같이 사용한다.
/// </summary>
public class QuadTree {

    const int LEFT_TOP = 1;
    const int LEFT_BOTTOM = 2;
    const int RIGHT_TOP = 3;
    const int RIGHT_BOTTOM = 4;

    public class Node<T>
    {
        public class Pair<T1, T2>
        {
            public T1 x { get; set; }
            public T2 y { get; set; }
        }

        public Pair<short, short> index { get; set; }
        public T data { get; set; }
        public Node<T> parent { get; set; }
        //public List<Node<T>> child { get; set; }
        public Node<T>[] child { get; set; }
        public short size;
        public short halfSize
        {
            get
            {
                return (short)(size / 2);
            }
        }

        public short quaterSize
        {
            get
            {
                return (short)(size / 4);
            }
        }

        public int IsInsert(short x, short y)
        {
            if (index.x == x && index.y == y)
            {
                return 0;
            }
            if (index.x > x)
            {
                if(index.y <= y)
                {
                    return LEFT_TOP;
                }
                else if(index.y > y)
                {
                    return LEFT_BOTTOM;
                }
                return -1;
            }
            else
            {
                if(index.y <= y)
                {
                    return RIGHT_TOP;
                }
                else if(index.y > y)
                {
                    return RIGHT_BOTTOM;
                }
                return -1;
            }
        }

        public bool isMatch(short x, short y)
        {
            if(isLeaf == false)
            {
                return false;
            }
            if(index.x == x && index.y == y)
            {
                return true;
            }
            return false;        
        }

        bool isLeaf
        {
            get
            {
                return size == 0;
            }
        }
    }

    public Node<short> Root;
    int MAX = 16;
    int MAX_HALF
    {
        get
        {
            return MAX / 2;
        }
    }
    
    public QuadTree()
    {
        Root = NewNode();
    }

    public QuadTree(short center_x, short center_y, int size = 64)
    {
        MAX = size;
        Root = NewNode();
        Root.index.x = center_x;
        Root.index.y = center_y;
        Root.size = (short)size;
        Root.data = -1;
    }

    public void Insert(short x, short y, short val)
    {
        if((Root.index.x + Root.halfSize -1)<x)
        {
            //x overflow
            return;
        }
        else if((Root.index.x - Root.halfSize) > x)
        {
            //x underflow
            return;
        }
        else if(Root.index.y + (Root.halfSize -1) < y)
        {
            //y overflow
            return;
        }
        else if(Root.index.y - Root.halfSize > y)
        {
            //y underflow
            return;
        }

        Insert(ref Root, x, y, val);
    }

    Node<short> Insert(ref Node<short> dest,short x, short y, short val)
    {
        var v = dest.IsInsert(x, y);
        if(v == 0)
        {
            //find it
            dest.data = val;
            return dest;
        }
        else if(dest.size == 1)
        {
            return null;
        }
        else
        {
            if(dest.child[v -1] == null)
            {
                dest.child[v - 1] = NewNode();
                if(dest.size > 2)
                {
                    dest.child[v - 1].index.x = (short)(dest.index.x + (dest.quaterSize * (short)((v <= 2) ? -1 : 1)));
                    dest.child[v - 1].index.y = (short)(dest.index.y + (dest.quaterSize * (short)((v % 2 == 0) ? -1 : 1)));
                }
                else
                {
                    switch(v)
                    {
                        case 1:
                            //Left top
                            dest.child[v-1].index.x = (short)(dest.index.x - 1);
                            dest.child[v - 1].index.y = dest.index.y;
                            break;
                        case 2:
                            //Left bottom
                            dest.child[v - 1].index.x = (short)(dest.index.x - 1);
                            dest.child[v - 1].index.y = (short)(dest.index.y - 1);
                            break;
                        case 3:
                            //Right top
                            dest.child[v - 1].index.x = dest.index.x;
                            dest.child[v - 1].index.y = dest.index.y;
                            break;
                        case 4:
                            //Right Bottom
                            dest.child[v - 1].index.x = dest.index.x;
                            dest.child[v - 1].index.y = (short)(dest.index.y - 1);
                            break;
                    }
                }
                dest.child[v - 1].size = (short)(dest.size / 2);
            }
            return Insert(ref dest.child[v - 1], x, y, val);
        }
    }

    public void Remove(int x, int y)
    {
        var n = FindNode(x, y);
        DeleteNode(ref n);
    }

    public short Find(int x, int y)
    {
        return Find(ref Root, x, y, MAX).data;
    }

    public Node<short> FindNode(int x, int y)
    {
        return Find(ref Root, x, y, MAX);
    }

    Node<short> Find(ref Node<short> dest, int x, int y, int size)
    {
        if (dest.index.x == x && dest.index.y == y)
        {
            return dest;
        }
        else
        {
            if (dest.index.x + size / 2 < x)
            {
                //left
                if (dest.index.y + size / 2 < y)
                {
                    //top
                    return Find(ref dest.child[LEFT_TOP], x, y, size / 2);
                }
                else
                {
                    //bottom
                    return Find(ref dest.child[LEFT_BOTTOM], x, y, size / 2);
                }
            }
            else
            {
                //right
                if (dest.index.y + size / 2 < y)
                {
                    //top
                    return Find(ref dest.child[RIGHT_TOP], x, y, size / 2);
                }
                else
                {
                    //bottom
                    return Find(ref dest.child[RIGHT_BOTTOM], x, y, size / 2);
                }
            }
        }
    }

    public void Set(int x, int y, short val)
    {
        var n = FindNode(x, y);
        n.data = val;        
    }

    Node<short> NewNode()
    {
        Node<short> node = new Node<short>();
        node.index = new Node<short>.Pair<short, short>();
        node.child = new Node<short>[4];
        return node;
    }
    
    void DeleteNode(ref Node<short> val)
    {
        val.child[0] = null;
        val.child[1] = null;
        val.child[2] = null;
        val.child[3] = null;

        for (int i = 0; i < val.parent.child.Length; i++)
        {
            if(val.parent.child[i] == val)
            {
                val.parent.child[i] = null;
                break;
            }
        }

        val.parent = null;
        val.index = null;
        val.data = 0;
        val.size = 0;
        val = null;
    }
}

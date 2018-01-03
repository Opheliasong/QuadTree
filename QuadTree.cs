using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Map 구조를 저장하는 QuadTree
/// 내부 node의 데이터는 short이며, 
/// 이 short의 value는 각 맵의 타일의 타입과 같이 사용한다.
/// </summary>
public class QuadTree<T> {

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

        public Pair<float, float> index { get; set; }
        public T data { get; set; }
        public Node<T> parent { get; set; }
        //public List<Node<T>> child { get; set; }
        public Node<T>[] child { get; set; }
        public float size;
        public int depth = 0;
        public float halfSize
        {
            get
            {
                return size / 2;
            }
        }

        public float quaterSize
        {
            get
            {
                return size / 4;
            }
        }

        public int IsInsert(float x, float y)
        {
            if (isMatch(x, y))
            {
                return 0;
            }

            if (index.x < x)
            {
                //right
                if (index.y < y)
                {
                    return RIGHT_TOP;
                }
                else
                {
                    return RIGHT_BOTTOM;
                }
            }
            else
            {
                //left
                if (index.y < y)
                {
                    return LEFT_TOP;
                }
                else
                {
                    return LEFT_BOTTOM;
                }
            }
        }

        public bool isMatch(float x, float y)
        {
            if (isLeaf == false)
            {
                return false;
            }
            if ((int)index.x == (int)x && (int)index.y == (int)y)
            {
                return true;
            }
            return false;
        }

        public bool isLeaf
        {
            get
            {
                return size == 1;
            }
        }
    }

    public Node<T> Root;
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
        Root.data = default(T);
    }

    public void Insert(float x, float y, T val)
    {
        if ((Root.index.x + Root.halfSize - 1) < x)
        {
            //x overflow
            return;
        }
        else if ((Root.index.x - Root.halfSize) > x)
        {
            //x underflow
            return;
        }
        else if (Root.index.y + (Root.halfSize - 1) < y)
        {
            //y overflow
            return;
        }
        else if (Root.index.y - Root.halfSize > y)
        {
            //y underflow
            return;
        }

        Insert(ref Root, x, y, val);
    }

    Node<T> Insert(ref Node<T> dest, float x, float y, T val)
    {
        var v = dest.IsInsert(x, y);
        if (v == 0)
        {
            //find it
            dest.data = val;
            return dest;
        }
        else if (dest.size == 1)
        {
            return null;
        }
        else
        {
            //Debug.Log("v is " + v);
            if (dest.child[v - 1] == null)
            {
                dest.child[v - 1] = NewNode();
                switch (v)
                {
                    case 1:
                        //Left top
                        dest.child[v - 1].index.x = (dest.index.x - dest.quaterSize);
                        dest.child[v - 1].index.y = (dest.index.y + dest.quaterSize);
                        break;
                    case 2:
                        //Left bottom
                        dest.child[v - 1].index.x = (dest.index.x - dest.quaterSize);
                        dest.child[v - 1].index.y = (dest.index.y - dest.quaterSize);
                        break;
                    case 3:
                        //Right top
                        dest.child[v - 1].index.x = (dest.index.x + dest.quaterSize);
                        dest.child[v - 1].index.y = (dest.index.y + dest.quaterSize);
                        break;
                    case 4:
                        //Right Bottom
                        dest.child[v - 1].index.x = (dest.index.x + dest.quaterSize);
                        dest.child[v - 1].index.y = (dest.index.y - dest.quaterSize);
                        break;
                }
                dest.child[v - 1].size = dest.halfSize;
                dest.child[v - 1].depth = dest.depth + 1;
            }
            return Insert(ref dest.child[v - 1], x, y, val);
        }
    }

    public void Remove(int x, int y)
    {
        var n = FindNode(x, y);
        DeleteNode(ref n);
    }

    public T Find(int x, int y)
    {
        return Find(ref Root, x, y, MAX).data;
    }

    public Node<T> FindNode(int x, int y)
    {
        return Find(ref Root, x, y, MAX);
    }

    Node<T> Find(ref Node<T> dest, int x, int y, int size)
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

    public void Set(int x, int y, T val)
    {
        var n = FindNode(x, y);
        n.data = val;
    }

    Node<T> NewNode()
    {
        Node<T> node = new Node<T>();
        node.index = new Node<T>.Pair<float, float>();
        node.child = new Node<T>[4];
        return node;
    }

    void DeleteNode(ref Node<T> val)
    {
        val.child[0] = null;
        val.child[1] = null;
        val.child[2] = null;
        val.child[3] = null;

        for (int i = 0; i < val.parent.child.Length; i++)
        {
            if (val.parent.child[i] == val)
            {
                val.parent.child[i] = null;
                break;
            }
        }

        val.parent = null;
        val.index = null;
        val.data = val.data;
        val.size = 0;
        val = null;
    }

    public Node<T> GetRoot()
    {
        return Root;
    }
}

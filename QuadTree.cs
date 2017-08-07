using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Map 구조를 저장하는 QuadTree
/// 내부 node의 데이터는 short이며, 
/// 이 short의 value는 각 맵의 타일의 타입과 같이 사용한다.
/// </summary>
public class QuadTree {

    const int RIGHT_TOP = 0;
    const int LEFT_TOP = 1;
    const int LEFT_BOTTOM = 2;
    const int RIGHT_BOTTOM = 3;

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
    }

    public Node<short> Root;
    int MAX = 64;
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
        Root = NewNode(center_x, center_y, (short)size);
        Root.data = -1;
    }

    public void Insert(short x, short y, short val)
    {
        Insert(ref Root, x, y, val);
    }

    Node<short> Insert(ref Node<short> dest,short x, short y, short val)
    {
        if(dest.size <= 0)
        {
            return null;
        }

        if(dest.index.x == x && dest.index.y == y)
        {
            dest.data = val;
            return dest;
        }
        
        if(dest.index.x <= x)
        {
            //Right
            if(dest.index.y < y)
            {
                //Top
                if(dest.child[RIGHT_TOP] == null)
                {
                    dest.child[RIGHT_TOP] = NewNode((short)(dest.index.x + dest.size/2), (short)(dest.index.y + dest.size/2), (short)(dest.size/2));
                    return Insert(ref dest.child[RIGHT_TOP], x, y, val);
                }
                else
                {
                    return Insert(ref dest.child[RIGHT_TOP], x, y, val);
                }
            }
            else
            {
                //Bottom
                if(dest.child[RIGHT_BOTTOM] == null)
                {
                    dest.child[RIGHT_BOTTOM] = NewNode((short)(dest.index.x + dest.size / 2), (short)(dest.index.y - dest.size / 2), (short)(dest.size / 2));
                    return Insert(ref dest.child[RIGHT_BOTTOM], x, y, val);
                }
                else
                {
                    return Insert(ref dest.child[RIGHT_BOTTOM], x, y, val);
                }
            }
        }
        else
        {
            //Left
            if (dest.index.y < y)
            {
                //Top
                if (dest.child[LEFT_TOP] == null)
                {
                    dest.child[LEFT_TOP] = NewNode((short)(dest.index.x - dest.size / 2), (short)(dest.index.y - dest.size / 2), (short)(dest.size / 2));
                    return Insert(ref dest.child[LEFT_TOP], x, y, val);
                }
                else
                {
                    return Insert(ref dest.child[LEFT_TOP], x, y, val);
                }
            }
            else
            {
                //Bottom
                if (dest.child[LEFT_BOTTOM] == null)
                {
                    dest.child[LEFT_BOTTOM] = NewNode((short)(dest.index.x - dest.size / 2), (short)(dest.index.y - dest.size / 2), (short)(dest.size / 2));
                    return Insert(ref dest.child[LEFT_BOTTOM], x, y, val);
                }
                else
                {
                    return Insert(ref dest.child[LEFT_BOTTOM], x, y, val);
                }
            }
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

    Node<short> NewNode(short x, short y, short size)
    {
        var node = NewNode();
        node.index.x = x;
        node.index.y = y;
        node.size = size;
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

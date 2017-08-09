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

        public bool IsInsert(short x, short y)
        {
            if(size > 1)
            {
                if(index.x + (size/2) < x)
                {
                    return false;
                }
                if(index.y + (size/2) < y)
                {
                    return false;
                }
                if(index.x - (size/2) > x)
                {
                    return false;
                }
                if(index.y - (size/2) > y)
                {
                    return false;
                }
            }
            //else
            //{
            //    if(index.x + 1 < x)
            //    {
            //        return false;
            //    }
            //    if(index.y + 1 < y)
            //    {
            //        return false;
            //    }
            //    if(index.x - 1 > x)
            //    {
            //        return false;
            //    }
            //    if(index.y - 1 > y)
            //    {
            //        return false;
            //    }
            //}
            return true;
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
        Insert(ref Root, x, y, val);
    }

    Node<short> Insert(ref Node<short> dest,short x, short y, short val)
    {        
        if(!dest.IsInsert(x, y))
        {
            return null;
        }

        if(dest.isMatch(x, y))
        {
            dest.data = val;
            return dest;
        }

        if(dest.index.x < x)
        {
            //Right
            if(dest.index.y < y)
            {
                //Top
                if(dest.child[RIGHT_TOP] == null)
                {
                    dest.child[RIGHT_TOP] = NewNode(ref dest, RIGHT_TOP, (short)(dest.index.x + dest.size/4), (short)(dest.index.y + dest.size/4), (short)(dest.size/2));
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
                    dest.child[RIGHT_BOTTOM] = NewNode(ref dest, RIGHT_BOTTOM, (short)(dest.index.x + dest.size / 4), (short)(dest.index.y - dest.size / 4), (short)(dest.size / 2));
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
                    dest.child[LEFT_TOP] = NewNode(ref dest, LEFT_TOP, (short)(dest.index.x - dest.size / 4), (short)(dest.index.y + dest.size / 4), (short)(dest.size / 2));
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
                    dest.child[LEFT_BOTTOM] = NewNode(ref dest, LEFT_BOTTOM, (short)(dest.index.x - dest.size / 4), (short)(dest.index.y - dest.size / 4), (short)(dest.size / 2));
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

    Node<short> NewNode(ref Node<short> parent, int index)
    {
        Node<short> node = NewNode();
        switch (index)
        {
            case LEFT_TOP:
                parent.child[LEFT_BOTTOM] = NewNode();
                parent.child[LEFT_BOTTOM].index.x = (short)(parent.index.x);
                parent.child[LEFT_BOTTOM].index.y = (short)(parent.index.y - 1);
                parent.child[LEFT_BOTTOM].size = 0;
                parent.child[LEFT_BOTTOM].parent = node;

                parent.child[LEFT_TOP] = NewNode();
                parent.child[LEFT_TOP].index.x = (short)(parent.index.x);
                parent.child[LEFT_TOP].index.y = (short)(parent.index.y);
                parent.child[LEFT_TOP].size = 0;
                parent.child[LEFT_TOP].parent = node;

                parent.child[RIGHT_TOP] = NewNode();
                parent.child[RIGHT_TOP].index.x = (short)(parent.index.x + 1);
                parent.child[RIGHT_TOP].index.y = (short)(parent.index.y);
                parent.child[RIGHT_TOP].size = 0;
                parent.child[RIGHT_TOP].parent = node;

                parent.child[RIGHT_BOTTOM] = NewNode();
                parent.child[RIGHT_BOTTOM].index.x = (short)(parent.index.x + 1);
                parent.child[RIGHT_BOTTOM].index.y = (short)(parent.index.y - 1);
                parent.child[RIGHT_BOTTOM].size = 0;
                parent.child[RIGHT_BOTTOM].parent = node;
                break;
            case LEFT_BOTTOM:
                parent.child[LEFT_BOTTOM] = NewNode();
                parent.child[LEFT_BOTTOM].index.x = (short)(parent.index.x);
                parent.child[LEFT_BOTTOM].index.y = (short)(parent.index.y);
                parent.child[LEFT_BOTTOM].size = 0;
                parent.child[LEFT_BOTTOM].parent = node;

                parent.child[LEFT_TOP] = NewNode();
                parent.child[LEFT_TOP].index.x = (short)(parent.index.x);
                parent.child[LEFT_TOP].index.y = (short)(parent.index.y+1);
                parent.child[LEFT_TOP].size = 0;
                parent.child[LEFT_TOP].parent = node;

                parent.child[RIGHT_TOP] = NewNode();
                parent.child[RIGHT_TOP].index.x = (short)(parent.index.x + 1);
                parent.child[RIGHT_TOP].index.y = (short)(parent.index.y + 1);
                parent.child[RIGHT_TOP].size = 0;
                parent.child[RIGHT_TOP].parent = node;

                parent.child[RIGHT_BOTTOM] = NewNode();
                parent.child[RIGHT_BOTTOM].index.x = (short)(parent.index.x + 1);
                parent.child[RIGHT_BOTTOM].index.y = (short)(parent.index.y);
                parent.child[RIGHT_BOTTOM].size = 0;
                parent.child[RIGHT_BOTTOM].parent = node;
                break;
            case RIGHT_TOP:
                parent.child[LEFT_BOTTOM] = NewNode();
                parent.child[LEFT_BOTTOM].index.x = (short)(parent.index.x - 1);
                parent.child[LEFT_BOTTOM].index.y = (short)(parent.index.y - 1);
                parent.child[LEFT_BOTTOM].size = 0;
                parent.child[LEFT_BOTTOM].parent = node;

                parent.child[LEFT_TOP] = NewNode();
                parent.child[LEFT_TOP].index.x = (short)(parent.index.x - 1);
                parent.child[LEFT_TOP].index.y = (short)(parent.index.y);
                parent.child[LEFT_TOP].size = 0;
                parent.child[LEFT_TOP].parent = node;

                parent.child[RIGHT_TOP] = NewNode();
                parent.child[RIGHT_TOP].index.x = (short)(parent.index.x);
                parent.child[RIGHT_TOP].index.y = (short)(parent.index.y);
                parent.child[RIGHT_TOP].size = 0;
                parent.child[RIGHT_TOP].parent = node;

                parent.child[RIGHT_BOTTOM] = NewNode();
                parent.child[RIGHT_BOTTOM].index.x = (short)(parent.index.x);
                parent.child[RIGHT_BOTTOM].index.y = (short)(parent.index.y-1);
                parent.child[RIGHT_BOTTOM].size = 0;
                parent.child[RIGHT_BOTTOM].parent = node;
                break;
            case RIGHT_BOTTOM:
                parent.child[LEFT_BOTTOM] = NewNode();
                parent.child[LEFT_BOTTOM].index.x = (short)(parent.index.x - 1);
                parent.child[LEFT_BOTTOM].index.y = (short)(parent.index.y);
                parent.child[LEFT_BOTTOM].size = 0;
                parent.child[LEFT_BOTTOM].parent = node;

                parent.child[LEFT_TOP] = NewNode();
                parent.child[LEFT_TOP].index.x = (short)(parent.index.x - 1);
                parent.child[LEFT_TOP].index.y = (short)(parent.index.y + 1);
                parent.child[LEFT_TOP].size = 0;
                parent.child[LEFT_TOP].parent = node;

                parent.child[RIGHT_TOP] = NewNode();
                parent.child[RIGHT_TOP].index.x = (short)(parent.index.x);
                parent.child[RIGHT_TOP].index.y = (short)(parent.index.y + 1);
                parent.child[RIGHT_TOP].size = 0;
                parent.child[RIGHT_TOP].parent = node;

                parent.child[RIGHT_BOTTOM] = NewNode();
                parent.child[RIGHT_BOTTOM].index.x = (short)(parent.index.x);
                parent.child[RIGHT_BOTTOM].index.y = (short)(parent.index.y);
                parent.child[RIGHT_BOTTOM].size = 0;
                parent.child[RIGHT_BOTTOM].parent = node;
                break;
        }
        return node;
    }

    Node<short> NewNode(ref Node<short> parent, int index, short x, short y, short size)
    {        
        var node = NewNode();
        node.index.x = x;
        node.index.y = y;
        node.size = size;
        node.parent = parent;

        if(size == 0)
        {
            NewNode(ref parent, index);
        }
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

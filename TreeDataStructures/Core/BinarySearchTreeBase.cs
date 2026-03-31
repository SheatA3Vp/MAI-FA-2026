using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys {
        get {
            var keys = new List<TKey>(Count);
            foreach (var e in InOrder()) keys.Add(e.Key);
            return keys;
        }
    }
    public ICollection<TValue> Values {
        get {
            var values = new List<TValue>(Count);
            foreach (var e in InOrder()) values.Add(e.Value);
            return values;
        }
    }
    
    
    public virtual void Add(TKey key, TValue value)
    {
        TNode? newNode = CreateNode(key, value);

        if (Root == null) {
            Root = newNode;
            Count++;
            OnNodeAdded(newNode);
            return;
        }

        TNode? parent = Root;
        TNode? current = Root;

        while (current != null) {
            parent = current;
            int cmp = Comparer.Compare(key, parent.Key);
            if (cmp < 0) current = current.Left;
            else if (cmp > 0) current = current.Right;
            // При дублировании ключа изменяем значение в ноде на новое
            // Запускать OnNodeAdded()? Создать хук OnNodeUpdated()?
            else {
                current.Value = value;
                return;
            }
        }

        newNode.Parent = parent;
        if (Comparer.Compare(key, parent.Key) < 0) parent.Left = newNode;
        else parent.Right = newNode;

        Count++; 
        OnNodeAdded(newNode);
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        TNode? current = null;

        if (node.Left == null && node.Right == null) {
            Transplant(node, null);
            OnNodeRemoved(node.Parent, null);
            return;
        }

        else if (node.Left == null && node.Right != null) {
            current = node.Right;
            Transplant(node, current);
        }

        else if (node.Left != null && node.Right == null) {
            current = node.Left;
            Transplant(node, current);
        }

        // Добавить Transplant
        else {
            current = node.Right;
            while (current.Left != null) {
                current = current.Left;
            }

            if (current.Parent != node) {
                Transplant(current, current.Right);
                current.Right = node.Right;
                if (current.Right != null) current.Right.Parent = current;
            }

            Transplant(node, current);
            current.Left = node.Left;
            if (current.Left != null) current.Left.Parent = current;
        }

        OnNodeRemoved(current.Parent, current);
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        var c = x.Right;
        if (c == null) throw new InvalidOperationException("RotateLeft: missing right child");

        x.Right = c.Left;
        if (c.Left != null) c.Left.Parent = x;

        c.Parent = x.Parent;
        if (x.Parent == null) Root = c;
        else if (x.IsLeftChild) x.Parent.Left = c;
        else x.Parent.Right = c;

        c.Left = x;
        x.Parent = c;
    }

    protected void RotateRight(TNode y)
    {
        var c = y.Left;
        if (c == null) throw new InvalidOperationException("RotateRight: missing left child");

        y.Left = c.Right;
        if (c.Right != null) c.Right.Parent = y;

        c.Parent = y.Parent;
        if (y.Parent == null) Root = c;
        else if (y.IsLeftChild) y.Parent.Left = c;
        else y.Parent.Right = c;

        c.Right = y;
        y.Parent = c;
    }
    
    protected void RotateBigLeft(TNode x)
    {
        if (x.Parent == null || x.Left == null || x.IsLeftChild) throw new InvalidOperationException("RotateBigLeft: invalid state");

        RotateRight(x);
        if (x.Parent == null || x.Parent.Parent == null) throw new InvalidOperationException("RotateBigLeft: no grandparent");
        RotateLeft(x.Parent.Parent);
    }
    
    protected void RotateBigRight(TNode y)
    {
        if (y.Parent == null || y.Right == null || !y.IsLeftChild) throw new InvalidOperationException("RotateBigRight: invalid state");

        RotateLeft(y);
        if (y.Parent == null || y.Parent.Parent == null) throw new InvalidOperationException("RotateBigRight: no grandparent");
        RotateRight(y.Parent.Parent);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        if (x.Parent == null || x.Parent.Parent == null) throw new InvalidOperationException("RotateDoubleLeft: no grandparent");
        if (x.IsLeftChild || x.Parent.IsLeftChild) throw new InvalidOperationException("RotateDoubleLeft: wrong side");

        RotateLeft(x.Parent);
        if (x.Parent == null || x.Parent.Parent == null) throw new InvalidOperationException("RotateDoubleLeft: no grandparent after first rotation");
        RotateLeft(x.Parent);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        if (y.Parent == null || y.Parent.Parent == null) throw new InvalidOperationException("RotateDoubleRight: no grandparent");
        if (!y.IsLeftChild || !y.Parent.IsLeftChild) throw new InvalidOperationException("RotateDoubleRight: wrong side");

        RotateRight(y.Parent);
        if (y.Parent == null || y.Parent.Parent == null) throw new InvalidOperationException("RotateDoubleRight: no grandparent after first rotation");
        RotateRight(y.Parent);
    }
    
    // Заменяем поддерево u на поддерево v 
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrder() => InOrderTraversal(Root);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrder() => PreOrderTraversal(Root);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrder() => PostOrderTraversal(Root);
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrderReverse() => InOrderReverseTraversal(Root);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrderReverse() => PreOrderReverseTraversal(Root);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrderReverse() => PostOrderReverseTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>>  InOrderTraversal(TNode? node)
    {
        if (node == null) {  return []; }
        return new TreeIterator(node, TraversalStrategy.InOrder);
    }

    private IEnumerable<TreeEntry<TKey, TValue>>  PreOrderTraversal(TNode? node)
    {
        if (node == null) {  return []; }
        return new TreeIterator(node, TraversalStrategy.PreOrder);
    }

    private IEnumerable<TreeEntry<TKey, TValue>>  PostOrderTraversal(TNode? node)
    {
        if (node == null) {  return []; }
        return new TreeIterator(node, TraversalStrategy.PostOrder);
    }

    private IEnumerable<TreeEntry<TKey, TValue>>  InOrderReverseTraversal(TNode? node)
    {
        if (node == null) {  return []; }
        return new TreeIterator(node, TraversalStrategy.InOrderReverse);
    }

    private IEnumerable<TreeEntry<TKey, TValue>>  PreOrderReverseTraversal(TNode? node)
    {
        if (node == null) {  return []; }
        return new TreeIterator(node, TraversalStrategy.PreOrderReverse);
    }

    private IEnumerable<TreeEntry<TKey, TValue>>  PostOrderReverseTraversal(TNode? node)
    {
        if (node == null) {  return []; }
        return new TreeIterator(node, TraversalStrategy.PostOrderReverse);
    }
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        TNode? _root;
        TNode? _current;
        private TreeEntry<TKey, TValue>? _currentEntry;
        private bool _started;

        private TraversalStrategy _strategy; // or make it template parameter?

        private void Init(TNode? root, TraversalStrategy strategy) {
            _root = root;
            _strategy = strategy;
            _current = _strategy switch {
                TraversalStrategy.InOrder => MostLeft(root),
                TraversalStrategy.PreOrder => root,
                TraversalStrategy.PostOrder => MostLeft(root),
                TraversalStrategy.InOrderReverse => MostRight(root),
                TraversalStrategy.PreOrderReverse => LastNodeInPreOrder(root),
                TraversalStrategy.PostOrderReverse => root,
                _ => throw new InvalidOperationException("Strategy not implemented")
            };

            _started = false;
            _currentEntry = null;
        }

        public TreeIterator(TNode? root, TraversalStrategy strategy) {  // Конструктор итератора
            Init(root, strategy);
        }
        
        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;

        public TreeEntry<TKey, TValue> Current {
            get {
                if (this._currentEntry.HasValue) return this._currentEntry.Value;
                throw new InvalidOperationException("Enumeration not positioned");
            }
        }
        object IEnumerator.Current => Current;

        private TNode? MostLeft(TNode? root) {
            if (root == null) return null;

            while (root.Left != null) {
                root = root.Left;
            }
            return root;
        }

        private TNode? MostRight(TNode? root) {
            if (root == null) return null;

            while (root.Right != null) {
                root = root.Right;
            }
            return root;
        }

        private TNode? LastNodeInPreOrder(TNode? root) {
            if (root == null) return null;
            while (root.Right != null || root.Left != null) {
                root = root.Right ?? root.Left;
            }
            return root;
        }
        
        
        public bool MoveNext()
        {
            if (!_started)
            {
                _started = true;
                if (_current == null)
                {
                    _currentEntry = null;
                    return false;
                }

                _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                return true;
            }

            if (_current == null)
            {
                _currentEntry = null;
                return false;
            }

            switch (_strategy){
                case TraversalStrategy.InOrder:  // left -> root -> right
                    if (_current.Right != null)
                    {
                        _current = MostLeft(_current.Right);
                        _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                        return true;
                    }
                    else
                    {
                        while (_current.Parent != null && !_current.IsLeftChild)
                        {
                            _current = _current.Parent;
                        }

                        if (_current.Parent == null) {
                            _currentEntry = null;
                            return false;
                        }
                        _current = _current.Parent;
                        _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                        return true;
                    }

                case TraversalStrategy.PreOrder:  // root -> left -> right
                    if (_current.Left != null) {
                        _current = _current.Left;
                        _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                        return true;

                    } else if (_current.Right != null) {
                        _current = _current.Right;
                        _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                        return true;

                    } else {
                        if (_current.Parent == null) {
                            _currentEntry = null;
                            return false;
                        }

                        while (!_current.IsLeftChild && _current.Parent != null) _current = _current.Parent;
                        while (_current.IsLeftChild && _current.Parent.Right == null) _current = _current.Parent;
                        if (_current.Parent == null) {
                            _current = null;
                            _currentEntry = null;
                            return false;
                        }

                        if (_current.Parent.Right != null) _current = _current.Parent.Right;

                        _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                        return true;
                    }

                case TraversalStrategy.PostOrder:  // left -> right -> root
                    if (_current == _root) {
                        _current = null;
                        _currentEntry = null;
                        return false;
                    }
                    if (_current.IsLeftChild) {
                        if (_current.Parent.Right == null) {
                            _current = _current.Parent;
                            _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                            return true;
                        } else {
                            _current = _current.Parent.Right;
                            while (_current.Left != null) _current = _current.Left;
                            while (_current.Right != null) _current = _current.Right;
                            _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                            return true;
                        }
                    } else {
                        if (_current.Parent != null){
                            _current = _current.Parent;
                            _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                            return true;
                        } else {
                            return false;
                        }
                    }

                case TraversalStrategy.InOrderReverse:  // right -> root -> left
                    if (_current.Left != null)
                    {
                        _current = MostRight(_current.Left);
                        _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                        return true;
                    }
                    else
                    {
                        while (_current.Parent != null && _current.IsLeftChild)
                        {
                            _current = _current.Parent;
                        }

                        if (_current.Parent == null) {
                            _currentEntry = null;
                            return false;
                        }
                        _current = _current.Parent;
                        _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                        return true;
                    }

                case TraversalStrategy.PreOrderReverse:  // right -> left -> root
                    if (_current == _root || _current.Parent == null) {
                        _current = null;
                        _currentEntry = null;
                        return false;
                    }
                    // Если мы левый ребенок, родитель был прямо перед нами
                    // То же самое, если мы правый, но левого брата нет
                    if (_current.IsLeftChild || _current.Parent.Left == null) {
                        _current = _current.Parent;
                    } else {
                        // Если мы правый ребенок и есть левый брат, 
                        // то идем в самый низ левого брата
                        _current = LastNodeInPreOrder(_current.Parent.Left);
                    }
                    _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                    return true;


                case TraversalStrategy.PostOrderReverse:  // root -> right -> left
                    if (_current.Right != null) {
                        _current = _current.Right;
                        _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                        return true;

                    } else if (_current.Left != null) {
                        _current = _current.Left;
                        _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                        return true;

                    } else {
                        var node = _current;
                        while (node.Parent != null) {
                            if (!node.IsLeftChild) {
                                var leftSibling = node.Parent.Left;
                                if (leftSibling != null && leftSibling != node) {
                                    _current = leftSibling;
                                    _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetDepth(_current));
                                    return true;
                                }
                            }
                            node = node.Parent;
                        }

                        _current = null;
                        _currentEntry = null;
                        return false;
                    }

                default:
                    throw new InvalidOperationException("Strategy not implemented");
            }
        }
        
        public void Reset()
        {
            Init(_root, _strategy);
        }

        
        public void Dispose()
        {
            // nothing to release
        }

        private int GetDepth(TNode? node)
        {
            int depth = 0;
            while (node != null && node.Parent != null)
            {
                depth++;
                node = node.Parent;
            }
            return depth;
        }
    }
    

    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        var list = new List<KeyValuePair<TKey, TValue>>(Count);

        foreach (var e in InOrder())
            list.Add(new KeyValuePair<TKey, TValue>(e.Key, e.Value));

        return list.GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
        if (array == null) throw new ArgumentNullException(nameof(array), "CopyTo: null array");
        if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex), "CopyTo: negative index");
        if (arrayIndex + Count > array.Length) throw new ArgumentException("CopyTo: insufficient space", nameof(arrayIndex));

        foreach (var e in InOrder()) {
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(e.Key, e.Value);
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}
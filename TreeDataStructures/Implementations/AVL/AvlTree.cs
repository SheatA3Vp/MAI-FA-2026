using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);


    protected int Abs(int x) {
        x = x < 0 ? -x : x;
        return x;
    }

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        if (newNode == Root) return;

        var current = newNode;
        var parent = newNode.Parent;

        while (current != null && parent != null) {

            if (current.IsLeftChild) ++parent.Diff;
            else --parent.Diff;

            if (parent.Diff == 0) {
                return;
            } else if (Abs(parent.Diff) == 2) {
                if (parent.Diff == -2) {  // current - правый ребенок. Малый левый поворот.

                    if (current.Diff == -1) {
                        RotateLeft(parent);
                        parent.Diff = 0;
                        current.Diff = 0;
                        return;
                    } else if (current.Diff == 0) {
                        RotateLeft(parent);
                        parent.Diff = -1;
                        current.Diff = 1;
                        
                        parent = current.Parent;

                    } else {  // Большой левый поворот
                        int child_diff = current.Left.Diff;
                        RotateBigLeft(current);
                        if (child_diff == 1) {
                            parent.Diff = 0;
                            current.Diff = -1;
                        } else if (child_diff == -1) {
                            parent.Diff = 1;
                            current.Diff = 0;
                        } else {
                            parent.Diff = 0;
                            current.Diff = 0;
                        }

                        parent = current.Parent;
                        parent.Diff = 0;
                        return;
                    }

                } else if (parent.Diff == 2) {  // current - левый ребенок. Малый правый поворот.

                    if (current.Diff == 1) {
                        RotateRight(parent);
                        parent.Diff = 0;
                        current.Diff = 0;
                        return;
                    } else if (current.Diff == 0) {

                        RotateRight(parent);
                        parent.Diff = 1;
                        current.Diff = -1;

                        parent = current.Parent;
                    
                    } else {  // Большой правый поворот
                        int child_diff = current.Right.Diff;
                        RotateBigRight(current);
                        if (child_diff == -1) {
                            parent.Diff = 0;
                            current.Diff = 1;
                        } else if (child_diff == 1) {
                            parent.Diff = -1;
                            current.Diff = 0;
                        } else {
                            parent.Diff = 0;
                            current.Diff = 0;
                        }

                        parent = current.Parent;
                        parent.Diff = 0;
                        return;
                    }
                }

                if (parent.Diff == 0) return;
            }

            current = current.Parent;
            parent = current?.Parent;
        }
    }


    protected override void OnNodeReplaced(AvlNode<TKey, TValue> oldNode, AvlNode<TKey, TValue> newNode) {
        newNode.Diff = oldNode.Diff;
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child) {
        if (parent == null) return;

        if (child == null) {
            if (parent.Left == null && parent.Right == null) {
                parent.Diff = 0;
            } else if (parent.Left == null) {
                parent.Diff = -1;
            } else {
                parent.Diff = 1;
            }
        } else {
            if (child.IsLeftChild) --parent.Diff;
            else ++parent.Diff;
        }

        var current = parent;

        while (current != null) {
            if (Abs(current.Diff) == 1) {
                return;
            }
            else if (Abs(current.Diff) == 2) {
                if (current.Diff == -2) {  // Перевес вправо, значит удалили левого
                    var sibling = current.Right;
                    if (sibling.Diff == -1) {
                        RotateLeft(current);
                        current.Diff = 0;
                        sibling.Diff = 0;
                        current = sibling;

                    } else if (sibling.Diff == 0) {
                        RotateLeft(current);
                        current.Diff = -1;
                        sibling.Diff = 1;
                        return; // Баланс не 0, останавливаемся

                    } else {
                        int grandsonDiff = sibling.Left?.Diff ?? 0;  // Если левый сын у sibling-a null, то значение ставим 0
                        RotateBigLeft(sibling);
                        if (grandsonDiff == 1) {
                            current.Diff = 0;
                            sibling.Diff = -1;
                        } else if (grandsonDiff == -1) {
                            current.Diff = 1;
                            sibling.Diff = 0;
                        } else {
                            current.Diff = 0;
                            sibling.Diff = 0;
                        }
                        current = current.Parent;
                        current.Diff = 0;
                    }
                } 
                else {  // current.Diff == 2, Перевес влево, удалили правого
                    var sibling = current.Left;
                    if (sibling.Diff == 1) {
                        RotateRight(current);
                        current.Diff = 0;
                        sibling.Diff = 0;
                        current = sibling;

                    } else if (sibling.Diff == 0) {
                        RotateRight(current);
                        current.Diff = 1;
                        sibling.Diff = -1;
                        return; // Баланс не 0, останавливаемся

                    } else {
                        int grandsonDiff = sibling.Right?.Diff ?? 0;
                        RotateBigRight(sibling);
                        if (grandsonDiff == -1) {
                            current.Diff = 0;
                            sibling.Diff = 1;
                        } else if (grandsonDiff == 1) {
                            current.Diff = -1;
                            sibling.Diff = 0;
                        } else {
                            current.Diff = 0;
                            sibling.Diff = 0;
                        }
                        current = current.Parent;
                        current.Diff = 0;
                    }
                }
            }

            // Если баланс current стал равным нулю (высота поддерева уменьшилась), продолжаем подъём
            var nextParent = current.Parent;
            if (nextParent != null) {
                if (current.IsLeftChild) --nextParent.Diff;
                else ++nextParent.Diff;
            }
            current = nextParent;
        }
    }
}
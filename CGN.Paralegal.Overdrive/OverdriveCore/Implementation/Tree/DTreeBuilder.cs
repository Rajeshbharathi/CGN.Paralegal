using System;
using System.Collections.Generic;
using System.Text;

namespace PH.DataTree
{
   /// <summary>
   /// Stateful helper class to build simple tree structures.</br>
   /// Provides the following methods:
   /// <list>
   /// <item>Add: Adds one or more nodes at the current level</item>
   /// <item>AddWithChild: Adds a new node and goes down one level</item>
   /// <item>Down: goes down one level</item>
   /// <item>Up: goes up one level</item>
   /// <item>sets the current level to the childs of the root node</item>
   /// <item>ToTree: resets the tree builder and returns the tree that was built</item>
   /// </list>
   /// 
   /// The TreeBuilder always generates a root node, and starts with root.Nodes as
   /// current level.
   /// 
   /// </summary>
   public class DTreeBuilder<T>
   {
      private DTreeNode<T> root;
      private DTreeNode<T> current;

      public DTreeBuilder()
      {
         Reset();
      }

      public DTreeBuilder(T rootValue)
      {
         Reset();
         SetRootValue(rootValue);
      }

      public void Reset()
      {
         root = new DTreeNode<T>();
         current = root;
      }

      public DTreeNode<T> ToTree()
      {
         DTreeNode<T> ret = root;
         Reset();
         return ret;
      }

      public DTreeBuilder<T> Add(T value)
      {
         current.Nodes.Add(value);
         return this;
      }

      public DTreeBuilder<T> Add(IEnumerable<T> values)
      {
         current.Nodes.AddRange(values);
         return this;
      }

      public DTreeBuilder<T> Add(params T[] args)
      {
         return Add(args as IEnumerable<T>);
      }

      public DTreeBuilder<T> Down()
      {
         current = current.Nodes[current.Nodes.Count - 1];
         return this;
      }

      public DTreeBuilder<T> AddWithChild(T value)
      {
         Add(value);
         Down();
         return this;
      }

      public DTreeBuilder<T> Up()
      {
         current = current.Parent;
         return this;
      }

      public DTreeBuilder<T> Root()
      {
         current = root;
         return this;
      }

      public DTreeBuilder<T> SetRootValue(T value)
      {
         root.Value = value;
         return this;
      }
   }

}

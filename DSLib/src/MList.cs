// (C) 2018-2023, single-linked list

namespace DiscScript
{
	using System.Collections;

	public class MConcurrentModificationException : MException
	{
		public MConcurrentModificationException() : base(MError.LIST, "concurrent modification")
		{
		}
	}

	public class MListNode<T>
	{
		public T? value;
		public MListNode<T>? Next;
	}

	public class MListIterator<T>
	{
		private int listState;
		private MList<T> list;
		private MListNode<T>? previous, current, next;

		/// <summary>Content of the current node.</summary>
		public T Value
		{
			get
			{
				if (current == null || current.value == null) throw new MException(MError.LIST, "");
				return current.value;
			}
		}
		public void SetValue(T v)
		{
			if (current == null) throw new MException(MError.LIST, "");
			_StateCheck();
			current.value = v;
		}
		public MListIterator(MList<T> _list, MListNode<T> _previous, int state)
		{
			listState = state;
			list = _list;
			previous = _previous;
			current = null;
			next = _previous.Next;
		}
		public bool HasValue()
		{
			_StateCheck();
			return current != null;
		}
		public bool HasNext()
		{
			_StateCheck();
			return next != null;
		}
		/// <summary>Step to next node. Return true if there was next.</summary>
		public bool Next()
		{
			_StateCheck();
			if (next == null) return false;
			if (current != null) previous = current; // current is null at the beginning and after Remove
			current = next;
			next = current.Next;
			return true;
		}
		/// <summary>Step to next node, or first if we're at the end. True if list is not empty.</summary>
		public bool Loop()
		{
			_StateCheck();
			if (list.Size() == 0) return false;
			if (next == null)
			{
				previous = list.Head();
				current = null;
				next = previous.Next;
				AssertValid();
				bool b = Next();
				MS.Assertion(b, MError.LIST, "Loop error");
				AssertValid();
				return true;
			}
			if (current != null) previous = current; // current is null at the beginning and after Remove
			current = next;
			next = current.Next;
			return true;
		}
		public void Remove()
		{
			_StateCheck();
			MS.Assertion(current != null, MError.LIST, "Remove: current not null");
			list.Remove(this);
			_StateRefresh();
		}
		public void InsertAfter(T t)
		{
			if (current == null) throw new MException(MError.LIST, "");
			_StateCheck();
			list.InsertAfter(current, t);
			_StateRefresh();
			Next();
		}
		public void InsertBefore(T t)
		{
			_StateCheck();
			MS.Assertion(current != null, MError.LIST, "InsertBefore: current not null"); // can't be beginning or end of list, or just deleted
			if (previous == null) throw new MException(MError.LIST, "");
			previous = list.InsertAfter(previous, t); // add new node and update iterator's previous
			_StateRefresh();
		}
		public bool Finished()
		{	
			_StateCheck();
			return next == null;
		}
		public void _Skip() // internal operation to do on Remove
		{
			if (previous == null) throw new MException(MError.LIST, "");
			if (next == null)
			{
				previous.Next = null; // end of list
				next = current = previous = null;
				return;
			}
			previous.Next = next;
			current = null;
		}
		public MListNode<T> _CurrentNode()
		{
			if (previous == null) throw new MException(MError.LIST, "");
			return previous;
		}
		public MListNode<T> _PreviousNode()
		{
			if (previous == null) throw new MException(MError.LIST, "");
			return previous;
		}
		private void _StateRefresh()
		{
			listState = list._State(); // do it when modified the list itself
		}
		private void _StateCheck()
		{
			if (listState != list._State()) throw new MConcurrentModificationException();
		}
		private void _StateAssertion()
		{
			MS.Assertion(listState == list._State(), MError.LIST, "_StateAssertion");
		}
		public void AssertValid()
		{
			_StateAssertion();
			if (previous != null)
			{
				if (current == null) MS.Assertion(previous.Next == next, MError.LIST, "AssertValid");
				else MS.Assertion(previous.Next == current, MError.LIST, "AssertValid");
			}
			else if (current != null)
			{
				MS.Assertion(current.Next == next, MError.LIST, "AssertValid");
			}
			else
			{
				MS.Assertion(next == null, MError.LIST, "AssertValid");
			}
		}
	}

	// standard enumerator for 'foreach' e.g.
	public class MListEnumerator<T> : IEnumerable<T>
	{
		private MList<T> list; MListIterator<T> it;
		public MListEnumerator(MList<T> _list) { list = _list; it = list.Iterator(); }
		IEnumerator IEnumerable.GetEnumerator()	{ yield return list.GetEnumerator(); }
		IEnumerator<T> IEnumerable<T>.GetEnumerator() { while (it.Next()) yield return it.Value; }
		public T Current { get { return it.Value; } }
		public bool MoveNext() { return it.Next(); }
	}


	public class MList<T>
	{
		private int size = 0, state = 0;
		private MListNode<T> head; // first, empty (dummy) node
		private MListNode<T>? tail; // last node, containing real data
	
		// TODO: iterator for internal use to prevent 'new' calls

		public MList() { head = new MListNode<T>(); tail = null; }
		public MListEnumerator<T> GetEnumerator() {	return new MListEnumerator<T>(this); }
		public MListIterator<T> Iterator()
		{
			return new MListIterator<T>(this, head, state);
		}
		public T First()
		{
			if (head.Next == null || head.Next.value == null) throw new MException(MError.LIST, "");
			return head.Next.value;
		}
		public T Last()
		{
			if (tail == null || tail.value == null) throw new MException(MError.LIST, "");
			return tail.value;
		}
		public int Size() { return size; }
		public bool IsEmpty() { return size == 0; }
		public int _State() { return state; }
		public MListNode<T> Head()
		{
			return head;
		}
		public void AddFirst(T t)
		{
			state++;
			if (Size() == 0)
			{
				AddLast(t);
				return;
			}
			var it = Iterator();
			it.Next();
			it.InsertBefore(t);
		}
		public void Add(T t)
		{
			AddLast(t);
		}
		public void AddLast(T t)
		{
			state++;
			MListNode<T> node = new MListNode<T> { value = t };
			if (size == 0) head.Next = node;
			else
			{
				if (tail == null) throw new MException(MError.LIST, "");
				tail.Next = node;
			}
			tail = node;
			size++;
		}
		public void RemoveFirst()
		{
			state++;
			if (size == 0) return;
			if (size == 1) head.Next = tail = null;
			else
			{
				if (head.Next == null) throw new MException(MError.LIST, "");
				head.Next = head.Next.Next;
			}
			size--;
		}
		//public void RemoveLast()
		//{
		//	// Difficult because new tail can't be defined
		//}
		public void RemoveAll()
		{
			while (size > 0) RemoveFirst();
		}
		public void Remove(MListIterator<T> it)
		{
			state++;
			if (it._CurrentNode() == tail)
			{
				tail = it._PreviousNode();
			}
			it._Skip();
			size--;
			if (size == 0) head.Next = tail = null;
		}
		//public MListIterator<T> Iterator(T value)
		//{
		//	var it = Iterator();
		//	while (it.Next())
		//	{
		//		if (it.Value != null && it.Value.Equals(value)) return it;
		//	}
		//	return null;
		//}
		public void Remove(T value)
		{
			var it = Iterator();
			while (it.Next())
			{
				if (it.Value != null && it.Value.Equals(value)) it.Remove();
			}
		}
		public int EqualIndex(T value, int minIndex = 0)
		{
			var it = Iterator();
			int index = 0;
			while (it.Next())
			{
				if (it.Value != null && it.Value.Equals(value) && index >= minIndex) return index;
				index++;
			}
			return -1;
		}
		public MListNode<T> InsertAfter(MListNode<T> position, T t)
		{
			// add a node after 'position', which can't be null
			// special cases: first, only one, last
			state++;
			MListNode<T> node = new MListNode<T> { value = t, Next = position.Next };
			position.Next = node;
			if (tail == position) tail = node;
			size++;
			return node;
		}
		public bool Contains(T x)
		{
			return Find(x) >= 0;
		}
		public int Find(T x)
		{
			// return first index of _x_ or -1 if not found
			int n = 0;
			var it = Iterator();
			while (it.Next())
			{
				if (it.Value != null && it.Value.Equals(x)) return n;
				n++;
			}
			return -1;
		}
		public T GetAt(int index)
		{
			MS.Assertion(index >= 0 && index < size, MError.LIST, "GetAt: index out of bounds, index: " + index + ", size: " + size);
			var it = Iterator();
			while (index-- >= 0) it.Next();
			return it.Value;
		}
		public void RemoveAt(int index)
		{
			MS.Assertion(index >= 0 && index < size, MError.LIST, "RemoveAt: index out of bounds, index: " + index + ", size: " + size);
			var it = Iterator();
			while (index-- >= 0) it.Next();
			it.Remove();
		}
		public T[] ToArray()
		{
			T[]a = new T[size];
			int i = 0;
			var it = Iterator();
			while (it.Next())
			{
				a[i] = it.Value;
				i++;
			}
			return a;
		}
		public void AssertValid()
		{
			if (size == 0)
			{
				MS.Assertion(tail == null, MError.LIST, "AssertValid");
				MS.Assertion(head.Next == null, MError.LIST, "AssertValid");
			}
			else
			{
				int n = 0;
				var it = Iterator();
				while (it.Next())
				{
					it.AssertValid();
					n++;
				}
				MS.Assertion(it.Value != null && tail != null && it.Value.Equals(tail.value), MError.LIST, "AssertValid");
				MS.Assertion(n == size, MError.LIST, "AssertValid");
				MS.Assertion(it.Finished(), MError.LIST, "AssertValid");
			}
		}
	}
}
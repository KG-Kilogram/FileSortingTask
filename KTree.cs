
using System.Text;

namespace FileSortingTask
{
    /// <summary>
    ///     Бинарное дерево с удержанием младшего элемента в корне.
    ///     Аглоритмы не используют рекурсию. 
    ///     С поддержкой повторяющихся элементов.
    /// </summary>
    public class KTree
    {
        /// <summary>
        ///     Корневой элемент. Если дерево содержит элементы, то в Root находится элемент с минимальным
        ///     KTreeElement.Hash
        /// </summary>
        private KTreeElement? Root = null;
        public bool Empty => Root is null;
        public List<IKTreeElementData>? MinHashDataList => Root?.DataList;


        public void Add(IKTreeElementData data)
        {
            if (Root is null)
            {
                Root = new KTreeElement(data);
                return;
            }

            if (Root.DataList[0].Hash > data.Hash)
            {
                // Изменение корня на элемент с меньшим Value
                var newRoot = new KTreeElement(data)
                {
                    RightElement = Root
                };

                Root = newRoot;
            }
            else
                Root.Add(data);
        }

        public void Add(IEnumerable<IKTreeElementData> dataList)
        {
            foreach (var data in dataList)
                Add(data);
        }

        private void Add(KTreeElement? element)
        {
            if (element is null)
                return;

            if (Root is null)
                Root = element;
            else
            {
                if (Root.DataList[0].Hash > element.DataList[0].Hash)
                {
                    var memRoot = Root;

                    Root = element;
                    Root.Add(memRoot);
                }
                else
                    Root.Add(element);
            }
        }

        private static IEnumerable<KTreeElement> GetLeftElementsChain(KTreeElement element)
        {
            var we = element;

            while (we.LeftElement is not null)
            {
                yield return we.LeftElement;
                we = we.LeftElement;
            }
        }

        /// <summary>
        ///     Перестраивает дерево таким образом, что в корне находится элемент с минимальным Hash    
        /// </summary>
        public void RefreshTree()
        {
            var memRoot = Root;
            Root = null;

            // 1. Если левых элементов нет, то корнем становится наименьший из правого поддерева
            if (memRoot!.LeftElement is null)
            {
                var rightElm = memRoot.RightElement;


                // 1.1 У корневого элемента нет ни левого ни правого поддерева, но могут быть
                // необработанные IKTreeElementData в списке DataList, которые образуют дерево
                // при добавлении.
                if (rightElm is null)
                    Add(memRoot.DataList.Where(d => !d.Empty));
                else if (rightElm.LeftElement is not null)
                {
                    // 1.2 У корневого элемента есть правое поддерево, а корень этого поддерева 
                    // имеет левый элемент (т.е. существует цепочка влево). Младший элемент - самый
                    // левый из них.

                    var leftElements = GetLeftElementsChain(rightElm).ToArray();

                    // В этом цикле дерево получит новы корень
                    for (int i = leftElements.Length - 1; i >= 0; i--)
                    {
                        leftElements[i].LeftElement = null;
                        Add(leftElements[i]);
                    }

                    rightElm.LeftElement = null;
                    Add(memRoot.RightElement);
                    Add(memRoot.DataList.Where(d => !d.Empty));
                }
                else
                {
                    // 1.2 У корневого элемента есть правое поддерево, а корень этого поддерева 
                    // не имеет левого элемента. Младший элемент - корень поддерева.

                    Root = memRoot.RightElement;
                    Add(memRoot.DataList.Where(d => !d.Empty));
                }
            }
            else
            {
                // 2. Левые элементы есть и корнем должен стать самый левый (младший)
                var leftElements = GetLeftElementsChain(memRoot).ToArray();

                // Перепостроение бинарного дерева с минимальным элементом в корне
                
                // В этом цикле дерево получит новы корень 
                for (int i = leftElements.Length - 1; i >= 0; i--)
                {
                    leftElements[i].LeftElement = null;
                    Add(leftElements[i]);
                }

                Add(memRoot.RightElement);
                Add(memRoot.DataList.Where(d => !d.Empty));
            }
        }

#if DEBUG
        private void GetDebugTreeString(KTreeElement elm, StringBuilder strbuilder, int lvl)
        {
            if (elm.LeftElement is not null)
            {
                strbuilder.Append(string.Format("{0}left element: {1}::\r\n",
                    new string(' ', lvl * 4), ((Page)elm.LeftElement.DataList[0]).Hash));

                GetDebugTreeString(elm.LeftElement, strbuilder, lvl + 1);
            }

            if (elm.RightElement is not null)
            {
                strbuilder.Append(string.Format("{0}right element: {1}::\r\n",
                    new string(' ', lvl * 4), ((Page)elm.RightElement.DataList[0]).Hash));

                GetDebugTreeString(elm.RightElement, strbuilder, lvl + 1);
            }
        }

        private bool BuildDebugTreeFirstTime = true;

        public string GetDebugTreeString()
        {
            if (BuildDebugTreeFirstTime)
            {
                Console.WriteLine("\r\n=======================================================================================");
                Console.WriteLine("=== DEBUG TREE BUILD IS ON ============================================================");
                Console.WriteLine("=======================================================================================\r\n");

                BuildDebugTreeFirstTime = false;
            }

            if (Root is not null)
            {                   
                StringBuilder strbuilder = new();
                
                strbuilder.Append(string.Format("root: {0}::\r\n", ((Page)Root!.DataList[0]).Hash));
                GetDebugTreeString(Root, strbuilder, 1);

                return strbuilder.ToString();
            }
            else
                return string.Empty;
        }
#endif

        private class KTreeElement
        {
            public KTreeElement(IKTreeElementData data)
            {
                DataList.Add(data);
            }

            public List<IKTreeElementData> DataList { get; set; } = new();

            public KTreeElement? LeftElement;
            public KTreeElement? RightElement;

            public void Add(IKTreeElementData data)
            {
                uint hash = data.Hash;
                KTreeElement element = this;

                while (true)
                {
                    if (hash < element.DataList[0].Hash)
                    {
                        if (element.LeftElement is null)
                        {
                            element.LeftElement = new KTreeElement(data);
                            break;
                        }
                        else
                            element = element.LeftElement;
                    }
                    else if (hash > element.DataList[0].Hash)
                    {
                        if (element.RightElement is null)
                        {
                            element.RightElement = new KTreeElement(data);
                            break;
                        }
                        else
                            element = element.RightElement;
                    }
                    else
                    {
                        (element.DataList ??= new()).Add(data);
                        break;
                    }
                }
            }

            public void Add(KTreeElement element)
            {
                uint hash = element.DataList[0].Hash;
                KTreeElement welement = this;

                while (true)
                {
                    if (hash < welement.DataList[0].Hash)
                    {
                        if (welement.LeftElement is null)
                        {
                            welement.LeftElement = element;
                            break;
                        }
                        else
                            welement = welement.LeftElement;
                    }
                    else if (hash > welement.DataList[0].Hash)
                    {
                        if (welement.RightElement is null)
                        {
                            welement.RightElement = element;
                            break;
                        }
                        else
                            welement = welement.RightElement;
                    }
                    else
                    {
                        foreach (var data in element.DataList.ToArray())
                            (welement.DataList ??= new()).Add(data);

                        (welement.DataList ??= new()).Add(element.DataList[0]);

                        break;
                    }
                }
            }
        }
    }

    public interface IKTreeElementData
    {
        public UInt32 Hash { get; set; }
        public bool Empty { get; set; }
    }
}

using System.Collections;
using System.Diagnostics;

namespace DiscArchivingTool
{
    [DebuggerDisplay("{Name}   {Count}个子目录，{Files.Count}个文件")]
    public class FreeFileSystemTree : IReadOnlyList<FreeFileSystemTree>
    {
        private FreeFileSystemTree(FreeFileSystemTree parent, string name) 
        {
            Parent = parent;
            Name= name;
        }
        public static FreeFileSystemTree CreateRoot()
        {
            return new FreeFileSystemTree(null, null);
        }

        public FreeFileSystemTree AddChild(string name)
        {
            var subTree = new FreeFileSystemTree(this, name);
            Directories.Add(subTree);
            return subTree;
        }
        public FreeFileSystemTree AddFile(string name)
        {
            var file=new FreeFileSystemTree(this, name);
            file.IsFile = true;
            Files.Add(file);
            return file;
        }


        public DiscFile File { get; set; }
        public bool IsFile { get; private set; } = false;

        public List<FreeFileSystemTree> Files { get; private set; } = new List<FreeFileSystemTree>();
        public List<FreeFileSystemTree> Directories { get; private set; }=new List<FreeFileSystemTree>();

        public IEnumerable<FreeFileSystemTree> All => Directories.Concat(Files);

        public bool IsEmpty
        {
            get
            {
                return (Files == null || Files.Count == 0) && (Directories == null || Directories.Count == 0);
            }
        }

        public int Count => Directories.Count;


        public IReadOnlyList<FreeFileSystemTree> GetAllFiles()
        {
            List<FreeFileSystemTree> files = new List<FreeFileSystemTree>();
            Get(this);
            return files.AsReadOnly();

            void Get(FreeFileSystemTree tree)
            {
                if (tree.Files != null && tree.Files.Count > 0)
                {
                    files.AddRange(tree.Files);
                }
                if (tree.Directories != null && tree.Directories.Count > 0)
                {
                    foreach (var subTree in tree)
                    {
                        Get(subTree);
                    }
                }
            }
        }

        public FreeFileSystemTree Parent { get; private set; }
        public string Name { get; }

        public FreeFileSystemTree this[int index] => Directories[index];

        public IEnumerator<FreeFileSystemTree> GetEnumerator() => new FreeFileSystemTreeEnumerator(All.ToList());

        public class FreeFileSystemTreeEnumerator : IEnumerator<FreeFileSystemTree>
        {
            public FreeFileSystemTreeEnumerator(List<FreeFileSystemTree> files)
            {
                FileDirs = files;
            }

            private int currentIndex = -1;
            public List<FreeFileSystemTree> FileDirs { get; private set; }
            public FreeFileSystemTree Current => FileDirs[currentIndex];

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                currentIndex++;
                return currentIndex < FileDirs.Count;
            }

            public void Reset() => currentIndex = -1;

            public void Dispose()
            {
            }
        }


        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DiscArchivingTool.App;

namespace DiscArchivingTool
{
    public class UpdateUtility : DiscUtilityBase
    {
        public List<UpdatingDiscFile> UpdatingDiscFiles { get; private set; }
        private List<DiscFile> discFiles;

        /// <summary>
        /// 搜索光盘和参考目录中的文件，进行匹配和属性差异查询
        /// </summary>
        /// <param name="discDir"></param>
        /// <param name="referenceDir"></param>
        /// <param name="byName"></param>
        /// <param name="byTime"></param>
        /// <param name="byLength"></param>
        public void Search(string discDir, string referenceDir, bool byName, bool byTime, bool byLength)
        {
            discFiles = ReadFileList(discDir).Values.Single();
            var dir = new DirectoryInfo(referenceDir);
            Dictionary<long, object> time2file = new Dictionary<long, object>();
            Dictionary<string, object> name2file = new Dictionary<string, object>();
            Dictionary<long, object> length2file = new Dictionary<long, object>();
            foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (byName)
                {
                    AddToDictionary(name2file, file.Name, file);
                }
                if (byTime)
                {
                    AddToDictionary(time2file, file.LastWriteTime.Ticks / 10000000L, file);
                }
                if (byLength)
                {
                    AddToDictionary(length2file, file.Length, file);
                }
            }

            //寻找光盘文件和参照文件在指定规则下相同的文件
            UpdatingDiscFiles = new List<UpdatingDiscFile>();
            List<DiscFile> matchedFiles = new List<DiscFile>();
            HashSet<string> okFiles = new HashSet<string>();//没有发生属性修改的文件
            foreach (var file in discFiles)
            {
                matchedFiles.Clear();
                bool found = true;
                if (byName)
                {
                    found = AddToMatchedFiles(matchedFiles, name2file, file.RawName);
                }
                if (found && byLength)
                {
                    found = AddToMatchedFiles(matchedFiles, length2file, file.Length);
                }
                if (found && byTime)
                {
                    found = AddToMatchedFiles(matchedFiles, time2file, file.LastWriteTime.Ticks / 10000000L);
                }

                //找到了匹配的文件
                if (found)
                {
                    Debug.Assert(matchedFiles.Count > 0);
                    bool first = true;
                    foreach (var matchedFile in matchedFiles)
                    {
                        //寻找是否有不匹配的项
                        UpdatingType type = UpdatingType.None;
                        if (matchedFile.RawName != file.RawName)
                        {
                            type |= UpdatingType.NameChanged;
                        }
                        if (matchedFile.Length != file.Length)
                        {
                            type |= UpdatingType.LengthChanged;
                        }
                        if ((matchedFile.LastWriteTime - file.LastWriteTime).Duration().TotalSeconds > Configs.MaxTimeTolerance)
                        {
                            type |= UpdatingType.TimeChanged;
                        }
                        if (matchedFile.Path != file.Path)
                        {
                            type |= UpdatingType.PathChanged;
                        }
                        //如果有属性不匹配
                        if (type > UpdatingType.None)
                        {
                            //属性不匹配，有可能是一个光盘文件中源文件名对应了多个参考目录中的文件，这个时候需要进行额外的判断。
                            if (!okFiles.Contains(file.DiscName))//保证没有属性未变动的文件。
                            {
                                UpdatingDiscFiles.Add(new UpdatingDiscFile()
                                {
                                    DiscFile = file,
                                    ReferenceFile = matchedFile,
                                    Type = type,
                                    Checked = first
                                });
                                first = false;
                            }
                        }
                        else //属性完全一致，不需要更新
                        {
                            okFiles.Add(file.DiscName); //加入无属性修改的文件集合中
                            //如果之前已经加入了不匹配的文件，那需要删除，肯定是在最后
                            while (UpdatingDiscFiles.Count > 0 && UpdatingDiscFiles[^1].DiscFile.DiscName == file.DiscName)
                            {
                                UpdatingDiscFiles.RemoveAt(UpdatingDiscFiles.Count - 1);
                            }
                        }
                    }
                }
                else //文件被删除
                {
                    UpdatingDiscFiles.Add(new UpdatingDiscFile()
                    {
                        DiscFile = file,
                        Type = UpdatingType.Deleted,
                        Checked = true
                    });
                }
            }

            //将枚举的源目录中的文件放到字典中
            void AddToDictionary<T>(Dictionary<T, object> dic, T key, FileInfo fileInfo)
            {
                DiscFile file = new DiscFile()
                {
                    RawName = fileInfo.Name,
                    Path = Path.GetRelativePath(referenceDir, fileInfo.FullName),
                    LastWriteTime = fileInfo.LastWriteTime,
                    Length = fileInfo.Length,
                };
                if (dic.ContainsKey(key))//如果字典已存在key，则value转为List
                {
                    if (dic[key] is List<DiscFile> lst)
                    {
                        lst.Add(file);
                    }
                    else if (dic[key] is DiscFile f)
                    {
                        dic[key] = new List<DiscFile>() { f, file };
                    }
                }
                else//字典不存在key，直接添加
                {
                    dic.Add(key, file);
                }
            }

            //将光盘文件根据规则在字典中寻找匹配的文件
            static bool AddToMatchedFiles<T>(List<DiscFile> matchedFiles, Dictionary<T, object> dic, T key)
            {
                if (dic.ContainsKey(key))//匹配
                {
                    if (matchedFiles.Count == 0) //第一个遇到的规则
                    {
                        if (dic[key] is List<DiscFile> lst)
                        {
                            matchedFiles.AddRange(lst);
                        }
                        else if (dic[key] is DiscFile f)
                        {
                            matchedFiles.Add(f);
                        }
                    }
                    else//不是第一个遇到的规则
                    {
                        if (dic[key] is List<DiscFile> lst)
                        {
                            var intersection = matchedFiles.Intersect(lst);
                            if (intersection.Any())//交集不为空
                            {
                                matchedFiles.Clear();
                                matchedFiles.AddRange(intersection);
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else if (dic[key] is DiscFile f)
                        {
                            if (matchedFiles.Contains(f))
                            {
                                matchedFiles.Clear();
                                matchedFiles.Add(f);
                            }
                            else//交集为空，不匹配
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                }
                else//不匹配
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 生成新的FileList
        /// </summary>
        /// <param name="outputDir"></param>
        /// <exception cref="Exception"></exception>
        public void Update(string outputDir)
        {
            var groups = UpdatingDiscFiles.Where(p => p.Checked).GroupBy(p => p.DiscFile.DiscName);
            if (groups.Any(p => p.Skip(1).Any()))
            {
                var file = groups.First(p => p.Skip(1).Any()).Key;
                throw new Exception("部分光盘文件对应了超过1个参照文件：" + file);
            }

            var name2UpdatingFile = groups.ToDictionary(p => p.Key, p => p.First());

            var newFiles = new List<DiscFile>();
            foreach (var file in discFiles)
            {
                if (name2UpdatingFile.ContainsKey(file.DiscName))
                {
                    var updatingFile = name2UpdatingFile[file.DiscName];

                    //删除
                    if (updatingFile.Type.HasFlag(UpdatingType.Deleted))
                    {
                        continue;
                    }

                    //文件名或路径改动
                    if (updatingFile.Type.HasFlag(UpdatingType.NameChanged) || updatingFile.Type.HasFlag(UpdatingType.PathChanged))
                    {
                        file.RawName = updatingFile.ReferenceFile.RawName;
                        file.Path = updatingFile.ReferenceFile.Path;
                    }
                    //其他改动无能为力
                }
                newFiles.Add(file);

            }


            string fileListName = $"filelist-{DateTime.Now:yyyyMMddHHmmss}.txt";
            using var fileListStream = File.OpenWrite(Path.Combine(outputDir, fileListName));
            using var writer = new StreamWriter(fileListStream);

            writer.WriteLine($"{newFiles.Select(p => p.LastWriteTime).OrderBy(p => p).First().ToString(DateTimeFormat)}\t" +
                $"{newFiles.Select(p => p.LastWriteTime).OrderByDescending(p => p).First().ToString(DateTimeFormat)}\t" +
                $"{newFiles.Sum(p => p.Length)}");
            foreach (var file in newFiles)
            {
                writer.WriteLine($"{file.DiscName}\t{file.Path}\t{file.LastWriteTime.ToString(DateTimeFormat)}\t{file.Length}\t{file.Md5}");
            }

        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscArchivingTool
{
    public class UpdateUtility
    {
      public  List<UpdatingDiscFile> UpdatingDiscFiles { get;private set; }
        public void Search(string discDir, string sourceDir, bool byName, bool byTime, bool byLength)
        {
            var discFiles = FileUtility.ReadFileList(discDir).Values.Single();
            var dir = new DirectoryInfo(sourceDir);
            Dictionary<DateTime, object> time2file = new Dictionary<DateTime, object>();
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
                    AddToDictionary(time2file, file.LastWriteTime, file);
                }
                if (byLength)
                {
                    AddToDictionary(length2file, file.Length, file);
                }
            }

            UpdatingDiscFiles = new List<UpdatingDiscFile>();
            List<FileInfo> matchedFiles = new List<FileInfo>();
            foreach (var file in discFiles)
            {
                matchedFiles.Clear();
                bool found = true;
                if (byName)
                {
                    found = AddToMatchedFiles(matchedFiles, name2file,  file.RawName);
                }
                if(found&&byLength)
                {
                    found = AddToMatchedFiles(matchedFiles, length2file,  file.Length);
                }
                if (found && byTime)
                {
                    found = AddToMatchedFiles(matchedFiles, time2file,  file.LastWriteTime);
                }

                if(found)
                {
                    Debug.Assert(matchedFiles.Count > 0);
                    bool first = true;
                    foreach (var matchedFile in matchedFiles)
                    {
                        UpdatingType type = UpdatingType.None ;
                        if(matchedFile.Name!=file.RawName)
                        {
                            type |= UpdatingType.NameChanged;
                        }
                        if (matchedFile.Length != file.Length)
                        {
                            type|= UpdatingType.LengthChanged;
                        }
                        if(matchedFile.LastWriteTime != file.LastWriteTime)
                        {
                            type |= UpdatingType.TimeChanged;
                        }
                        if(Path.GetRelativePath(sourceDir, matchedFile.FullName)!=file.Path)
                        {
                            type |= UpdatingType.PathChanged;
                        }
                        if(type>UpdatingType.None)
                        {

                            UpdatingDiscFiles.Add(new UpdatingDiscFile()
                            {
                                File = file,
                                SourceFile = matchedFile,
                                Type = type,
                                Checked = first
                            });
                            first = false;
                        }
                    }
                }
                else //文件被删除
                {
                    UpdatingDiscFiles.Add(new UpdatingDiscFile()
                    {
                        File = file,
                        Type = UpdatingType.Deleted,
                        Checked = true
                    });
                }
            }

            //将枚举的源目录中的文件放到字典中
            static void AddToDictionary<T>(Dictionary<T, object> dic, T key, FileInfo value)
            {
                if (dic.ContainsKey(key))//如果字典已存在key，则value转为List
                {
                    if (dic[key] is List<FileInfo> lst)
                    {
                        lst.Add(value);
                    }
                    else if (dic[key] is FileInfo f)
                    {
                        dic[key] = new List<FileInfo>() { f, value };
                    }
                }
                else//字典不存在key，直接添加
                {
                    dic.Add(key, value);
                }
            }

            //将光盘文件根据规则在字典中寻找匹配的文件
            static bool AddToMatchedFiles<T>( List<FileInfo> matchedFiles, Dictionary<T,object> dic,T key)
            {
                if (dic.ContainsKey(key))//匹配
                {
                    if (matchedFiles.Count == 0) //第一个遇到的规则
                    {
                        if (dic[key] is List<FileInfo> lst)
                        {
                            matchedFiles.AddRange(lst);
                        }
                        else if (dic[key] is FileInfo f)
                        {
                            matchedFiles.Add(f);
                        }
                    }
                    else//不是第一个遇到的规则
                    {
                        if (dic[key] is List<FileInfo> lst)
                        {
                            var intersection= matchedFiles.Intersect(lst);
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
                        else if (dic[key] is FileInfo f)
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
    }
}

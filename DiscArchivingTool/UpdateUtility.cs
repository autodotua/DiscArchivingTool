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
        public void Search(string discDir, string referenceDir, bool byName, bool byTime, bool byLength)
        {
            var discFiles = FileUtility.ReadFileList(discDir).Values.Single();
            var dir = new DirectoryInfo(referenceDir);
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
            List<DiscFile> matchedFiles = new List<DiscFile>();
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

                //找到了匹配的文件
                if(found)
                {
                    Debug.Assert(matchedFiles.Count > 0);
                    bool first = true;
                    foreach (var matchedFile in matchedFiles)
                    {
                        //寻找是否有不匹配的项
                        UpdatingType type = UpdatingType.None ;
                        if(matchedFile.RawName!=file.RawName)
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
                        if(matchedFile.Path!=file.Path)
                        {
                            type |= UpdatingType.PathChanged;
                        }
                        if(type>UpdatingType.None)
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
            static bool AddToMatchedFiles<T>( List<DiscFile> matchedFiles, Dictionary<T,object> dic,T key)
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
    }
}

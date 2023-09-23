using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.Data;
using AmongUs.Data.Player;
using Assets.InnerNet;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Reflection;

namespace TOHE;

// ##https://github.com/Yumenopai/TownOfHost_Y
//来源：TOHY（谢谢！）
public class ModNewsSCn
{
    public int Number;
    public int BeforeNumber;
    public string Title;
    public string SubTitle;
    public string ShortTitle;
    public string Text;
    public string Date;

    public Announcement ToAnnouncement()
    {
        var result = new Announcement
        {
            Number = Number,

            Title = Title,
            SubTitle = SubTitle,
            ShortTitle = ShortTitle,
            Text = Text,
            Language = /*(uint)SupportedLangs.SChinese == */(uint)DataManager.Settings.Language.CurrentLanguage, 
            Date = Date,
            Id = "ModNews"
        };

        return result;
    }
}
[HarmonyPatch]
public class ModNewsHistorySCn
{
    public static List<ModNewsSCn> AllModNews = new();
    public static void Init()
    {
        //当你创建新的公告时，你不能删除旧的公告
        /*
        *每当你创建新公告时，请按照以下格式：
        {
        var news = new ModNews
            {
            Number = xxx,         //（编号）
            Title = "",           //（标题）
            SubTitle = "",        //（副标题）
            ShortTitle = "",      //（短标题）
            Test = ""            //（文本）
            + "\n"        
            ......
            + "\n",
        *注意，文本在最后一个+""以后添加逗号    
            Date = "",            //（日期）
            };
        AllModNews.Add(news);
        }
        *别忘了标点！！！！！
        */
        {
            var news = new ModNewsSCn
            {
                Number = 100000,
                Title = "TownOfHostEdited-Xi v2.0.2",
                SubTitle = "★★★★全新的TOHEX！★★★★",
                ShortTitle = "★TOHEX v2.0.2★\n<size=75%>简体中文</size>",
                Text = 
                "简体中文"
                + "\n-----------------------------"
                + "\n<size=125%>欢迎来到TOHEX,感谢您的游玩</size>"
                + "\n-----------------------------"
                + "\n全新的TOHEX!"
                + "\n\n\n## 对应官方版本"
                + "\n- 基于TOH版本 v4.1.2"
                + "\n- 基于TOHE版本 v2.3.55"
                + "\n- 适配Among Us版本 v2023.7.11及以上版本"
                + "\n\n## 修复"
                + "\n- 修复亨利等名字提示无法显示问题"
                + "\n\n## 新增、回归职业"
                + "\n- 中立阵营：挑战者"
                     + "\n- 中立阵营：抗拒者"
                          + "\n- 内鬼阵营：击球手"
                               + "\n- 内鬼阵营：寻血者"
                                    + "\n- 中立阵营：挑战者"
                + "\n\n## 写在最后"
                + "\n事实上TOHEX已经凉了有一阵子了，"
                + "\n没人来测试，但有人来各种诋毁、甚至辱骂开发者"
                + "\n咔皮呆说的对，做模组真的很累"
                + "\n但愿200版本能让玩家回流一些吧"
                + "\n看到这里，我们要说一声谢谢，是你们给了我们开发的动力，"
                + "\n祝您游玩愉快！"
                + "\n                                                       ——TOHEX开发组",
                Date = "2023-8-25T00:00:00Z",
            };
            AllModNews.Add(news);
        }
        {
            var news = new ModNewsSCn
            {
                Number = 100001,
                Title = "TownOfHostEdited-Xi v2.0.0",
                SubTitle = "★★★★NEW TOHEX!★★★★",
                ShortTitle = "★TOHEX v2.0.0★\n<size=75%>English</size>",
                Text =
                "English"                + "\n-----------------------------"                + "\n<size=125%>Welcome To TOHEX,Thank ya For Playing!</size>"                + "\n-----------------------------"                + "\nNew TOHEX!"                + "\n\n\n## Support Among Us Version"                + "\n- Based On TOH v4.1.2"                + "\n- Based On TOHE v2.3.55"                + "\n- Support Among Us v2023.7.11 And Above"                + "\n\n## Bugs Fix"                + "\n- When Evil Mini Grows Up Reset Kill Cooldown Bug Fix"                + "\n- Sidekick Can't Become Jackal Bug Fix"                + "\n\n We're so sorry about we haven't completed English Translate yet and brings you terrible experience, we have no time"
                + "\n if you wanna help us, please PR your trans in Github, Thanks For your support!",
                Date = "2023-8-15T00:00:00Z",
            };
            AllModNews.Add(news);
        }
    }

    [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
    public static bool SetModAnnouncements(PlayerAnnouncementData __instance, [HarmonyArgument(0)] ref Il2CppReferenceArray<Announcement> aRange)
    {
        if (AllModNews.Count < 1)
        {
             Init();
            AllModNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });
        }

        List<Announcement> FinalAllNews = new();
        AllModNews.Do(n => FinalAllNews.Add(n.ToAnnouncement()));
        foreach (var news in aRange)
        {
            if (!AllModNews.Any(x => x.Number == news.Number))
                FinalAllNews.Add(news);
        }
        FinalAllNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });
        aRange = new(FinalAllNews.Count);
        for (int i = 0; i < FinalAllNews.Count; i++)
            aRange[i] = FinalAllNews[i];

        return true;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Senparc.Weixin.Entities;
using Senparc.Weixin.QY.Entities;
using Senparc.Weixin.QY.CommonAPIs;
using Senparc.Weixin.QY.AdvancedAPIs;
using Senparc.Weixin.QY.AdvancedAPIs.OAuth2;
using Senparc.Weixin.HttpUtility;
using System.IO;
using QJY.Data;
using Senparc.Weixin.QY.AdvancedAPIs.MailList;
using Senparc.Weixin.Work.AdvancedAPIs.OaDataOpen;
using QJY.Common;
using System.Text;

namespace QJY.API
{
    public class WXHelp
    {


        public JH_Auth_QY Qyinfo = null;

        public WXHelp(JH_Auth_QY QY)
        {
            //获取企业信息
            Qyinfo = QY;
        }


        public string GetToken(string appID = "")
        {

            if (Qyinfo.IsUseWX == "Y")
            {
                if (appID == "")
                {
                    AccessTokenResult Token = CommonApi.GetToken(Qyinfo.corpId.Trim(), Qyinfo.corpSecret.Trim());
                    return Token.access_token;
                }
                else
                {
                    JH_Auth_Model Model = new JH_Auth_ModelB().GetEntities(d => d.AppID == appID).FirstOrDefault();
                    string strcorpSecret = Qyinfo.corpSecret.Trim();
                    if (Model != null && !string.IsNullOrEmpty(Model.Remark1))
                    {
                        strcorpSecret = Model.Remark1.Trim();
                    }
                    AccessTokenResult Token = CommonApi.GetToken(Qyinfo.corpId.Trim(), strcorpSecret);
                    return Token.access_token;
                }
            }
            else
            {
                return "";
            }
        }

        public JsApiTicketResult GetTicket()
        {
            if (Qyinfo.IsUseWX == "Y")
            {
                JsApiTicketResult js = CommonApi.GetTicket(Qyinfo.corpId.Trim(), Qyinfo.corpSecret.Trim());
                return js;
            }
            return null;
        }

        public JsonGroupTicket GetGroup_Ticket()
        {
            string access_token = GetToken();
            var url = string.Format("https://qyapi.weixin.qq.com/cgi-bin/ticket/get?access_token={0}&type=contact",
               access_token);





            JsonGroupTicket js = Get.GetJson<JsonGroupTicket>(url);
            return js;
        }

        #region 消息相关
        public void SendTH(List<Article> MODEL, string ModelCode, string type, string strUserS = "@all")
        {
            try
            {
                var app = new JH_Auth_ModelB().GetEntity(p => p.ModelCode == ModelCode);

                if (strUserS == "")
                {
                    return;
                }
                thModel th = new thModel();
                th.MODEL = MODEL;
                th.authAppID = app.AppID;
                th.UserS = string.IsNullOrEmpty(strUserS) ? "@all" : strUserS;
                if (Qyinfo.IsUseWX == "Y")
                {
                    th.MODEL.ForEach(d => d.Url = Qyinfo.WXUrl.TrimEnd('/') + "/View_Mobile/UI/UI_COMMON.html?funcode=" + ModelCode + "_" + type + (d.Url == "" ? "" : "_" + d.Url) + "&corpid=" + Qyinfo.corpId.Trim());
                    th.MODEL.ForEach(d => d.PicUrl = (string.IsNullOrEmpty(d.PicUrl) ? "" : Qyinfo.FileServerUrl.Trim() + Qyinfo.QYCode + "/document/image/" + new FT_FileB().ExsSclarSql("select zyid from FT_File where ID='" + d.PicUrl + "'").ToString()));

                    //if (app.AppType == "1")
                    //{
                    MassApi.SendNews(GetToken(app.AppID.ToString()), th.UserS.Replace(',', '|'), "", "", app.AppID, th.MODEL);
                    //}
                    //else
                    //{
                    //    MassApi.SendText(GetToken(app.AppID.ToString()), th.UserS.Replace(',', '|'), "", "", app.AppID, th.MODEL[0].Title);
                    //}
                }
            }
            catch (Exception ex)
            {
                CommonHelp.WriteLOG(ex.ToString());
            }
        }

        public void SendTPMSG(string ModelCode, List<Article> MODEL, string strUserS = "@all")
        {
            try
            {
                var app = new JH_Auth_ModelB().GetEntity(p => p.ModelCode == ModelCode);

                if (strUserS == "")
                {
                    return;
                }
                if (Qyinfo.IsUseWX == "Y")
                {

                    MassApi.SendNews(GetToken(app.AppID.ToString()), strUserS, "", "", app.AppID, MODEL);
                }
            }
            catch { }
        }

        /// <summary>
        /// 文字消息
        /// </summary>
        /// <param name="MsgText"></param>
        /// <param name="strAPPID"></param>
        /// <param name="strUserS"></param>
        public void SendWXRText(string MsgText, string ModelCode, string strUserS = "@all")
        {
            try
            {
                var app = new JH_Auth_ModelB().GetEntity(p => p.ModelCode == ModelCode);

                if (strUserS == "")
                {
                    return;
                }
                if (Qyinfo.IsUseWX == "Y")
                {

                    MassApi.SendText(GetToken(app.AppID.ToString()), strUserS, "", "", app.AppID, MsgText);
                }
            }
            catch { }
        }
        /// <summary>
        /// 图片消息
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="strAPPID"></param>
        /// <param name="strUserS"></param>
        public void SendImage(string filePath, string strAPPID, string strUserS = "@all")
        {
            try
            {
                if (strUserS == "")
                {
                    return;
                }
                if (Qyinfo.IsUseWX == "Y")
                {
                    Senparc.Weixin.QY.AdvancedAPIs.Media.UploadTemporaryResultJson md = MediaApi.Upload(GetToken(), Senparc.Weixin.QY.UploadMediaFileType.image, filePath);
                    if (md.media_id != "")
                    {
                        MassApi.SendImage(GetToken(), strUserS, "", "", strAPPID, md.media_id);
                    }
                }
            }
            catch { }
        }
        //文件消息
        public void SendFile(string filePath, string strAPPID, string strUserS = "@all")
        {
            try
            {
                if (strUserS == "")
                {
                    return;
                }
                if (Qyinfo.IsUseWX == "Y")
                {
                    Senparc.Weixin.QY.AdvancedAPIs.Media.UploadTemporaryResultJson md = MediaApi.Upload(GetToken(), Senparc.Weixin.QY.UploadMediaFileType.file, filePath);
                    if (md.media_id != "")
                    {
                        MassApi.SendFile(GetToken(), strUserS, "", "", strAPPID, md.media_id);
                    }

                }
            }
            catch { }
        }

        #endregion

        #region 组织机构相关

        public string GetUserDataByCode(string strCode, string strModelCode)
        {
            string UserCode = "";
            strModelCode = strModelCode.Split('_')[0];
            try
            {
                if (Qyinfo.IsUseWX == "Y")
                {
                    JH_Auth_Model Model = new JH_Auth_ModelB().GetEntities(d => d.ModelCode == strModelCode).FirstOrDefault();
                    if (Model != null)
                    {
                        GetUserInfoResult OBJ = OAuth2Api.GetUserId(GetToken(Model.AppID), strCode);
                        UserCode = OBJ.UserId;

                    }
                }
            }
            catch (Exception EX)
            {
                new JH_Auth_LogB().Insert(new JH_Auth_Log() { CRDate = DateTime.Now, LogContent = strModelCode + "获取用户代码" + strCode + "|GetUserDataByCode" + EX.Message.ToString() });

            }

            return UserCode;
        }
        public int WX_CreateBranch(JH_Auth_Branch Model)
        {

            int pid = 0;
            var bm = new JH_Auth_BranchB().GetEntity(p => p.DeptCode == Model.DeptRoot && p.ComId == Model.ComId);
            if (bm != null)
            {
                pid = Int32.Parse(bm.WXBMCode.ToString());
            }
            return MailListApi.CreateDepartment(GetToken(), Model.DeptName, pid, Model.DeptShort, Model.WXBMCode).id;

        }
        //同步部门使用
        public int WX_CreateBranchTB(JH_Auth_Branch Model)
        {

            int pid = 0;
            var bm = new JH_Auth_BranchB().GetEntity(p => p.DeptCode == Model.DeptRoot && p.ComId == Model.ComId);
            if (bm != null)
            {
                pid = Int32.Parse(bm.WXBMCode.ToString());
            }
            return MailListApi.CreateDepartment(GetToken(), Model.DeptName, pid, Model.DeptShort).id;

        }
        public QyJsonResult WX_UpdateBranch(JH_Auth_Branch Model)
        {
            QyJsonResult Ret = new QyJsonResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                int pid = 0;
                var bm = new JH_Auth_BranchB().GetEntity(p => p.DeptCode == Model.DeptRoot && p.ComId == Model.ComId);
                if (bm != null)
                {
                    pid = Int32.Parse(bm.WXBMCode.ToString());
                }
                Ret = MailListApi.UpdateDepartment(GetToken(), Model.WXBMCode.ToString(), Model.DeptName, pid, Model.DeptShort);
            }
            return Ret;
        }

        public QyJsonResult WX_DelBranch(string strDeptCode)
        {
            QyJsonResult Ret = new QyJsonResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                Ret = MailListApi.DeleteDepartment(GetToken(), strDeptCode);
            }
            return Ret;
        }
        public GetDepartmentListResult WX_GetBranchList(string strDeptCode)
        {
            GetDepartmentListResult Ret = new GetDepartmentListResult();
            int? id = null;
            if (!string.IsNullOrEmpty(strDeptCode))
            {
                id = Int32.Parse(strDeptCode);
            }
            if (Qyinfo.IsUseWX == "Y")
            {
                Ret = MailListApi.GetDepartmentList(GetToken(), id);
            }
            return Ret;
        }



        public QyJsonResult WX_CreateUser(JH_Auth_User Model)
        {
            try
            {
                QyJsonResult Ret = new QyJsonResult();
                if (Qyinfo.IsUseWX == "Y")
                {
                    int[] Branch = { new JH_Auth_BranchB().GetEntity(d => d.DeptCode == Model.BranchCode).WXBMCode.Value };
                    Ret = MailListApi.CreateMember(GetToken(), Model.UserName, Model.UserRealName, Branch, Model.zhiwu, Model.mobphone, Model.mailbox, Model.weixinnum);
                }
                return Ret;
            }
            catch (Exception ex)
            {
                QyJsonResult Ret = new QyJsonResult();
                new QJY.API.JH_Auth_LogB().Insert(new QJY.Data.JH_Auth_Log() { CRDate = DateTime.Now, LogContent = Model.UserName + "新增错误：" + ex.ToString() });
                return Ret;
            }
        }
        /// <summary>
        /// 更新用户包括状态
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        public QyJsonResult WX_UpdateUser(JH_Auth_User Model)
        {
            try
            {
                QyJsonResult Ret = new QyJsonResult();
                if (Qyinfo.IsUseWX == "Y")
                {

                    int[] Branch = { new JH_Auth_BranchB().GetEntity(d => d.DeptCode == Model.BranchCode).WXBMCode.Value };
                    Ret = MailListApi.UpdateMember(GetToken(), Model.UserName, Model.UserRealName, Branch, Model.zhiwu, Model.mobphone, Model.mailbox, Model.weixinnum, Model.IsUse == "Y" ? 1 : 0);
                    string str = UpUserXB(Model.UserName, Model.UserRealName, Model.Sex == "男" ? "1" : "2");
                }
                return Ret;
            }
            catch (Exception ex)
            {
                QyJsonResult Ret = new QyJsonResult();
                new QJY.API.JH_Auth_LogB().Insert(new QJY.Data.JH_Auth_Log() { CRDate = DateTime.Now, LogContent = Model.UserName + "更新错误：" + ex.ToString() });
                return Ret;
            }
        }

        public QyJsonResult WX_DelUser(string strUserName)
        {
            QyJsonResult Ret = new QyJsonResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                Ret = MailListApi.DeleteMember(GetToken(), strUserName);
            }
            return Ret;
        }

        public QyJsonResult WX_GetDepartmentList()
        {
            QyJsonResult Ret = new QyJsonResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                Ret = MailListApi.GetDepartmentList(GetToken());
            }
            return Ret;
        }
        public QyJsonResult WX_GetDepartmentMember(int depid)
        {
            QyJsonResult Ret = new QyJsonResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                Ret = MailListApi.GetDepartmentMember(GetToken(), depid, 1, 0);
            }
            return Ret;
        }
        public GetDepartmentMemberInfoResult WX_GetDepartmentMemberInfo(int depid)
        {
            GetDepartmentMemberInfoResult Ret = new GetDepartmentMemberInfoResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                Ret = MailListApi.GetDepartmentMemberInfo(GetToken(), depid, 1, 0);
            }
            return Ret;
        }
        #endregion

        #region TAG管理
        public QyJsonResult WX_CreateTag(JH_Auth_Role Model)
        {
            QyJsonResult Ret = new QyJsonResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                Ret = MailListApi.CreateTag(GetToken(), Model.RoleName, Model.WXBQCode);
            }
            return Ret;
        }
        public QyJsonResult WX_UpdateTag(JH_Auth_Role Model)
        {
            QyJsonResult Ret = new QyJsonResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                int bqid = Int32.Parse(Model.WXBQCode.ToString());
                Ret = MailListApi.UpdateTag(GetToken(), bqid, Model.RoleName);
            }
            return Ret;
        }
        public QyJsonResult WX_DelTag(int strBQCode)
        {
            QyJsonResult Ret = new QyJsonResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                Ret = MailListApi.DeleteTag(GetToken(), strBQCode);
            }
            return Ret;
        }
        public QyJsonResult WX_AddTagMember(JH_Auth_UserRole Model)
        {
            var role = new JH_Auth_RoleB().GetEntity(p => p.RoleCode == Model.RoleCode);
            QyJsonResult Ret = new QyJsonResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                string[] userList = { Model.UserName };
                int bqid = Int32.Parse(role.WXBQCode.ToString());
                Ret = MailListApi.AddTagMember(GetToken(), bqid, userList, null);
            }
            return Ret;
        }
        public QyJsonResult WX_DelTagMember(int strBQCode, string[] userList)
        {
            QyJsonResult Ret = new QyJsonResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                Ret = MailListApi.DelTagMember(GetToken(), strBQCode, userList);
            }
            return Ret;
        }
        public GetTagListResult WX_GetTagList()
        {
            GetTagListResult Ret = new GetTagListResult();

            if (Qyinfo.IsUseWX == "Y")
            {
                Ret = MailListApi.GetTagList(GetToken());
            }
            return Ret;
        }
        public GetTagMemberResult WX_GetTagMember(int tagid)
        {
            GetTagMemberResult Ret = new GetTagMemberResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                Ret = MailListApi.GetTagMember(GetToken(), tagid);
            }
            return Ret;
        }
        #endregion





        #region 菜单相关
        public QyJsonResult WX_WxCreateMenuNew(int agentId, string ModelCode, ref List<Senparc.Weixin.QY.Entities.Menu.BaseButton> lm)
        {
            string strMenuURL = Qyinfo.WXUrl.TrimEnd('/') + "/View_Mobile/UI/UI_COMMON.html";
            QyJsonResult Ret = new QyJsonResult();
            if (Qyinfo.IsUseWX == "Y")
            {

                var list = new JH_Auth_CommonB().GetEntities(p => p.ModelCode == ModelCode && p.TopID == 0 && p.Type == "1").OrderBy(p => p.Sort);

                foreach (var l in list)
                {
                    string url = string.Empty;
                    string key = string.Empty;
                    if (string.IsNullOrEmpty(l.MenuCode))
                    {
                        url = strMenuURL + "?funcode=" + l.ModelCode + "&corpId=" + Qyinfo.corpId;
                        key = l.ModelCode;
                    }
                    else
                    {
                        url = strMenuURL + "?funcode=" + l.ModelCode + "_" + l.MenuCode + "&corpId=" + Qyinfo.corpId;
                        key = l.ModelCode + "_" + l.MenuCode;
                    }

                    var list1 = new JH_Auth_CommonB().GetEntities(p => p.ModelCode == ModelCode && p.TopID == l.ID && p.Type == "1").OrderBy(p => p.Sort);
                    if (list1.Count() == 0)
                    {
                        lm.Add(GetButton(l.Type, l.MenuName, url, key));
                    }
                    else
                    {
                        Senparc.Weixin.QY.Entities.Menu.SubButton scb = new Senparc.Weixin.QY.Entities.Menu.SubButton();
                        scb.name = l.MenuName;

                        foreach (var l1 in list1)
                        {
                            string url1 = string.Empty;
                            string key1 = string.Empty;
                            if (string.IsNullOrEmpty(l1.MenuCode))
                            {
                                url1 = strMenuURL + "?funcode=" + l1.ModelCode + "&corpId=" + Qyinfo.corpId;
                                key1 = l1.ModelCode;
                            }
                            else
                            {
                                url1 = strMenuURL + "?funcode=" + l1.ModelCode + "_" + l1.MenuCode + "&corpId=" + Qyinfo.corpId;
                                key1 = l1.ModelCode + "_" + l1.MenuCode;
                            }

                            switch (l1.Type)
                            {
                                case "1": //跳转URL
                                    Senparc.Weixin.QY.Entities.Menu.SingleViewButton svb = new Senparc.Weixin.QY.Entities.Menu.SingleViewButton();
                                    svb.name = l1.MenuName;
                                    svb.type = "view";
                                    svb.url = url1;

                                    scb.sub_button.Add(svb);
                                    break;
                                case "2": //点击推事件
                                    Senparc.Weixin.QY.Entities.Menu.SingleClickButton skb = new Senparc.Weixin.QY.Entities.Menu.SingleClickButton();
                                    skb.name = l1.MenuName;
                                    skb.type = "click";
                                    skb.key = key1;

                                    scb.sub_button.Add(skb);
                                    break;
                                case "3"://扫码推事件
                                    Senparc.Weixin.QY.Entities.Menu.SingleScancodePushButton spb = new Senparc.Weixin.QY.Entities.Menu.SingleScancodePushButton();
                                    spb.name = l1.MenuName;
                                    spb.type = "scancode_push";
                                    spb.key = key1;

                                    scb.sub_button.Add(spb);
                                    break;
                                case "4"://扫码推事件且弹出“消息接收中”提示框
                                    Senparc.Weixin.QY.Entities.Menu.SingleScancodeWaitmsgButton swb = new Senparc.Weixin.QY.Entities.Menu.SingleScancodeWaitmsgButton();
                                    swb.name = l1.MenuName;
                                    swb.type = "scancode_waitmsg";
                                    swb.key = key1;

                                    scb.sub_button.Add(swb);
                                    break;
                                case "5"://弹出系统拍照发图
                                    Senparc.Weixin.QY.Entities.Menu.SinglePicSysphotoButton ssb = new Senparc.Weixin.QY.Entities.Menu.SinglePicSysphotoButton();
                                    ssb.name = l1.MenuName;
                                    ssb.type = "pic_sysphoto";
                                    ssb.key = key1;

                                    scb.sub_button.Add(ssb);
                                    break;
                                case "6"://弹出拍照或者相册发图
                                    Senparc.Weixin.QY.Entities.Menu.SinglePicPhotoOrAlbumButton sab = new Senparc.Weixin.QY.Entities.Menu.SinglePicPhotoOrAlbumButton();
                                    sab.name = l1.MenuName;
                                    sab.type = "pic_photo_or_album";
                                    sab.key = key1;

                                    scb.sub_button.Add(sab);
                                    break;
                                case "7"://弹出微信相册发图器
                                    Senparc.Weixin.QY.Entities.Menu.SinglePicWeixinButton sxb = new Senparc.Weixin.QY.Entities.Menu.SinglePicWeixinButton();
                                    sxb.name = l1.MenuName;
                                    sxb.type = "pic_weixin";
                                    sxb.key = key1;

                                    scb.sub_button.Add(sxb);
                                    break;
                                case "8"://弹出地理位置选择器
                                    Senparc.Weixin.QY.Entities.Menu.SingleLocationSelectButton slb = new Senparc.Weixin.QY.Entities.Menu.SingleLocationSelectButton();
                                    slb.name = l1.MenuName;
                                    slb.type = "location_select";
                                    slb.key = key1;

                                    scb.sub_button.Add(slb);
                                    break;
                            }

                        }

                        lm.Add(scb);
                    }
                }
                if (lm.Count > 0)
                {
                    Senparc.Weixin.QY.Entities.Menu.ButtonGroup buttonData = new Senparc.Weixin.QY.Entities.Menu.ButtonGroup();
                    buttonData.button = lm;

                    //Ret = CommonApi.CreateMenu(accesstoken, agentId, buttonData);
                    Ret = WX_CreateMenu(agentId, buttonData);
                }
            }
            return Ret;
        }
        public QyJsonResult WX_CreateMenu(int agentId, Senparc.Weixin.QY.Entities.Menu.ButtonGroup buttonData)
        {
            QyJsonResult Ret = new QyJsonResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                Ret = CommonApi.CreateMenu(GetToken(agentId.ToString()), agentId, buttonData);
            }
            return Ret;
        }
        public GetMenuResult WX_GetMenu(int agentId)
        {
            GetMenuResult Ret = new GetMenuResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                Ret = CommonApi.GetMenu(GetToken(agentId.ToString()), agentId);
            }
            return Ret;
        }
        public QyJsonResult WX_DelMenu(int agentId)
        {
            QyJsonResult Ret = new QyJsonResult();
            if (Qyinfo.IsUseWX == "Y")
            {
                Ret = CommonApi.DeleteMenu(GetToken(agentId.ToString()), agentId);
            }
            return Ret;
        }
        #endregion



        public string GetMediaFile(string mediaId, string strType = ".jpg")
        {
            string path = HttpContext.Current.Server.MapPath("\\temp\\");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string mdfile = path + Guid.NewGuid().ToString() + strType;
            FileStream fs = new FileStream(mdfile, FileMode.Create);
            MediaApi.Get(GetToken(), mediaId, fs);
            fs.Close();
            return mdfile;
        }
        public Senparc.Weixin.QY.Entities.Menu.BaseButton GetButton(string type, string menuname, string url, string key)
        {
            Senparc.Weixin.QY.Entities.Menu.BaseButton bb = new Senparc.Weixin.QY.Entities.Menu.BaseButton();
            switch (type)
            {
                case "1": //跳转URL
                    Senparc.Weixin.QY.Entities.Menu.SingleViewButton svb = new Senparc.Weixin.QY.Entities.Menu.SingleViewButton();
                    svb.name = menuname;
                    svb.type = "view";
                    svb.url = url;

                    bb = svb;
                    break;
                case "2": //点击推事件
                    Senparc.Weixin.QY.Entities.Menu.SingleClickButton scb = new Senparc.Weixin.QY.Entities.Menu.SingleClickButton();
                    scb.name = menuname;
                    scb.type = "click";
                    scb.key = key;

                    bb = scb;
                    break;
                case "3"://扫码推事件
                    Senparc.Weixin.QY.Entities.Menu.SingleScancodePushButton spb = new Senparc.Weixin.QY.Entities.Menu.SingleScancodePushButton();
                    spb.name = menuname;
                    spb.type = "scancode_push";
                    spb.key = key;

                    bb = spb;
                    break;
                case "4"://扫码推事件且弹出“消息接收中”提示框
                    Senparc.Weixin.QY.Entities.Menu.SingleScancodeWaitmsgButton swb = new Senparc.Weixin.QY.Entities.Menu.SingleScancodeWaitmsgButton();
                    swb.name = menuname;
                    swb.type = "scancode_waitmsg";
                    swb.key = key;

                    bb = swb;
                    break;
                case "5"://弹出系统拍照发图
                    Senparc.Weixin.QY.Entities.Menu.SinglePicSysphotoButton ssb = new Senparc.Weixin.QY.Entities.Menu.SinglePicSysphotoButton();
                    ssb.name = menuname;
                    ssb.type = "pic_sysphoto";
                    ssb.key = key;

                    bb = ssb;
                    break;
                case "6"://弹出拍照或者相册发图
                    Senparc.Weixin.QY.Entities.Menu.SinglePicPhotoOrAlbumButton sab = new Senparc.Weixin.QY.Entities.Menu.SinglePicPhotoOrAlbumButton();
                    sab.name = menuname;
                    sab.type = "pic_photo_or_album";
                    sab.key = key;

                    bb = sab;
                    break;
                case "7"://弹出微信相册发图器
                    Senparc.Weixin.QY.Entities.Menu.SinglePicWeixinButton sxb = new Senparc.Weixin.QY.Entities.Menu.SinglePicWeixinButton();
                    sxb.name = menuname;
                    sxb.type = "pic_weixin";
                    sxb.key = key;

                    bb = sxb;
                    break;
                case "8"://弹出地理位置选择器
                    Senparc.Weixin.QY.Entities.Menu.SingleLocationSelectButton slb = new Senparc.Weixin.QY.Entities.Menu.SingleLocationSelectButton();
                    slb.name = menuname;
                    slb.type = "location_select";
                    slb.key = key;

                    bb = slb;
                    break;
            }
            return bb;
        }




        /// <summary>
        /// 获取微信审批数据
        /// </summary>
        /// <param name="strSDate"></param>
        /// <param name="strEDate"></param>
        /// <returns></returns>
        public GetApprovalDataJsonResult GetWXSHData(string strSDate, string strEDate, string strLastNum = "")
        {
            AccessTokenResult Token = CommonApi.GetToken(Qyinfo.corpId.Trim(), CommonHelp.GetConfig("WXLCDATA"));
            string strReturn = "";
            string access_token = Token.access_token;

            GetApprovalDataJsonResult obj = OaDataOpenApi.GetApprovalData(access_token, DateTime.Parse(strSDate), DateTime.Parse(strEDate), 0);

            return obj;
        }





        /// <summary>
        /// 修改用户性别
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="UserRealName"></param>
        /// <param name="gender">1为男，2为女</param>
        /// <returns></returns>
        public string UpUserXB(string UserName, string UserRealName, string gender = "1")
        {
            string responeJsonStr = "{";
            responeJsonStr += "\"userid\": \"" + UserName + "\",";
            responeJsonStr += "\"name\": \"" + UserRealName + "\",";
            responeJsonStr += "\"gender\": \"" + gender + "\"";
            responeJsonStr += "}";
            string accessToken = GetToken();
            string postUrl = string.Format("https://qyapi.weixin.qq.com/cgi-bin/user/update?access_token={0}", accessToken);
            return CommonHelp.PostWebRequest(postUrl, responeJsonStr, Encoding.UTF8);
        }






    }

    public class thModel
    {
        public List<Article> MODEL { get; set; }
        public string authAppID { get; set; }
        public int ID { get; set; }
        public string UserS { get; set; }
    }

    public class JsonGroupTicket
    {
        public string errcode { get; set; }
        public string errmsg { get; set; }
        public string group_id { get; set; }
        public string ticket { get; set; }
        public string expires_in { get; set; }

    }







    #region 微信扫码登录所需的类
    public class GetProviderToken
    {
        /// <summary>
        /// 服务提供商的accesstoken
        /// </summary>
        public string provider_access_token { get; set; }

        /// <summary>
        /// access_token超时时间
        /// </summary>
        public int expires_in { get; set; }
    }
    #endregion
}
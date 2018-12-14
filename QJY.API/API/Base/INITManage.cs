using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using FastReflectionLib;
using System.Data;
using QJY.Data;
using Newtonsoft.Json;
using Senparc.Weixin.QY.AdvancedAPIs.MailList;
using Senparc.Weixin.Entities;
using System.Text.RegularExpressions;
using Senparc.Weixin.QY.AdvancedAPIs.App;
using System.Diagnostics;
using System.Threading;
using QJY.Common;

namespace QJY.API
{
    public class INITManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(INITManage).GetMethod(msg.Action.ToUpper());
            INITManage model = new INITManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }

        #region 设置常用菜单显示
        //设置手机APP，PC首页菜单显示应用
        public void SETAPPINDEX(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string type = context.Request["type"] ?? "APPINDEX";//默认为APP首页显示菜单，传值为PC首页的快捷方式按钮
            foreach (string str in P1.Split(','))
            {
                string[] content = str.Split(':');
                string modelCode = content[0];
                //判断是否存在菜单的数据，存在只更新状态，不存在添加
                JH_Auth_UserCustomData customData = new JH_Auth_UserCustomDataB().GetEntity(d => d.UserName == UserInfo.User.UserName && d.DataType == type && d.ComId == UserInfo.User.ComId && d.DataContent == modelCode);
                string status = content[1];
                if (customData != null)
                {
                    customData.DataContent1 = status;
                    new JH_Auth_UserCustomDataB().Update(customData);
                }
                else
                {
                    customData = new JH_Auth_UserCustomData();
                    customData.ComId = UserInfo.User.ComId;
                    customData.UserName = UserInfo.User.UserName;
                    customData.CRDate = DateTime.Now;
                    customData.CRUser = UserInfo.User.UserName;
                    customData.DataContent = modelCode;
                    customData.DataContent1 = status;
                    customData.DataType = type;
                    new JH_Auth_UserCustomDataB().Insert(customData);
                }
                if (type == "APPINDEX")
                {
                    msg.Result = customData;
                }
            }


        }
        #endregion
        #region 常用菜单设置
        public void GETMOBILETJDATA(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            //获取有权限的角色APP
            DataTable dtModel = new JH_Auth_ModelB().GETMenuList(UserInfo, P1);
            DataView dv = new DataView(dtModel);
            //获取套件
            DataTable dtTJ = dv.ToTable(true, new string[] { "ModelType", "TJId" }).OrderBy("ModelType asc");
            dtTJ.Columns.Add("Model", Type.GetType("System.Object"));
            foreach (DataRow row in dtTJ.Rows)
            {
                string tjId = row["TJID"].ToString();
                if (P1 == "APPINDEX")
                {
                    row["Model"] = dtModel.FilterTable("TJId='" + tjId + "'");
                }
                else
                {
                    row["Model"] = dtModel.FilterTable("TJId='" + tjId + "'  and UserAPPID is null ");
                }

            }
            msg.Result = dtTJ;
            if (P1 == "PCINDEX")
            {
                msg.Result1 = dtModel.FilterTable(" UserAPPID is not null  ");
            }
        }
        /// <summary>
        /// 第五版的自定义显示菜单和左边菜单
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETINDEXMENUNEW(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            DataTable dtModel = new JH_Auth_ModelB().GETMenuList(UserInfo, P1);
            dtModel.Columns.Add("ISSY", Type.GetType("System.Int32"));
            dtModel.Columns.Add("FunData", typeof(DataTable));
            if (dtModel != null && dtModel.Rows.Count > 0)
            {  //获取用户设置首页显示APP
                List<string> userCustom = new JH_Auth_UserCustomDataB().GetEntities(d => d.ComId == UserInfo.User.ComId && d.UserName == UserInfo.User.UserName && d.DataType == P1 && d.DataContent1 == "Y").Select(d => d.DataContent).ToList();

                foreach (DataRow row in dtModel.Rows)
                {
                    if (userCustom.Count > 0)
                    {
                        row["ISSY"] = 0;
                        if (row["UserAPPID"].ToString() != "")
                        {
                            row["ISSY"] = 1;
                        }
                    }
                    else
                    {

                        row["ISSY"] = 1;
                    }
                    row["FunData"] = new JH_Auth_RoleB().GetModelFun(UserInfo.User.ComId.Value, UserInfo.UserRoleCode, row["ID"].ToString());

                }
            }
            msg.Result = dtModel;
            if (UserInfo.User.isSupAdmin == "Y" && UserInfo.QYinfo.SystemGGId != "Y")
            {
                msg.Result1 = "Y";
            }
            msg.Result2 = UserInfo.User.isSupAdmin;

        }
        #endregion

        #region 获取首页菜单
        /// <summary>
        /// 获取首页菜单
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETINDEXMENU(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            //获取有权限的角色APP
            DataTable dtMenu = new JH_Auth_ModelB().GETMenuList(UserInfo, P1);

            dtMenu.Columns.Add("FunData", typeof(DataTable));
            foreach (DataRow row in dtMenu.Rows)
            {
                if (UserInfo.UserRoleCode != "" && UserInfo.User.isSupAdmin != "Y")
                {
                    row["FunData"] = new JH_Auth_RoleB().GetModelFun(UserInfo.User.ComId.Value, UserInfo.UserRoleCode, row["ID"].ToString());
                }
                else
                {
                    row["FunData"] = new JH_Auth_RoleB().GetModelFun(UserInfo.User.ComId.Value, "0", row["ID"].ToString());
                }
            }
            //获取用户设置首页显示APP
            List<string> userCustom = new JH_Auth_UserCustomDataB().GetEntities(d => d.ComId == UserInfo.User.ComId && d.UserName == UserInfo.User.UserName && d.DataType == P1 && d.DataContent1 == "Y").Select(d => d.DataContent).ToList();

            dtMenu.Columns.Add("number");
            msg.Result = dtMenu;
            //如果用户未设置首页显示APP，只显示通讯录
            if (userCustom.Count > 0)
            {
                foreach (DataRow row in dtMenu.Rows)
                {
                    if (!userCustom.Contains(row["ModelCode"].ToString()))
                    {
                        row.Delete();
                    }
                }

                dtMenu.AcceptChanges();

                #region 获取每个应用的带处理任务数量
                string strsql = string.Format(@"SELECT wfpd.ProcessName ,COUNT(0) number from Yan_WF_TI wfti  inner join Yan_WF_PI  wfpi on wfti.PIID=wfpi.ID
                                    inner join Yan_WF_PD wfpd on wfpi.PDID=wfpd.ID  where wfti.ComId={0}    and wfti.TaskUserID='{1}' and  
                                    wfti.TDCODE not like '%-1'  and wfti.TaskState=0  group by wfpd.ProcessName,wfpd.ProcessType", UserInfo.User.ComId, UserInfo.User.UserName);
                DataTable dtLCSP = new SZHL_LCSPB().GetDTByCommand(strsql);

                string strSql1 = string.Format("SELECT '任务管理' ProcessName, COUNT(0) number from SZHL_RWGL where  RWStatus=0 and RWFZR='{0}' and ComId={1}", UserInfo.User.UserName, UserInfo.User.ComId);
                DataTable dtRWGL = new SZHL_RWGLB().GetDTByCommand(strSql1);
                if (dtRWGL.Rows.Count > 0)
                {
                    dtLCSP.Merge(dtRWGL);
                }
                #endregion
                //处理首页显示的APP代办数量
                foreach (DataRow row in dtMenu.Rows)
                {
                    //获取待处理数量
                    DataRow[] DBL = dtLCSP.Select(" ProcessName='" + row["ModelName"] + "'");
                    row["number"] = DBL.Length > 0 ? DBL[0]["number"] : 0;
                }
                msg.Result = dtMenu;
            }
            else if (userCustom.Count == 0 && P1 == "APPINDEX")
            {
                DataTable dtShowMenu = dtMenu.Clone();
                DataRow[] rowMenu = dtMenu.Rows.OfType<DataRow>().Skip(1).Take(20).ToArray();
                foreach (DataRow row in rowMenu)
                {
                    dtShowMenu.Rows.Add(row.ItemArray);
                }
                msg.Result = dtShowMenu;
            }
            //获取快捷方式的菜单
            DataRow[] dr = dtMenu.Select(" IsKJFS=1  or IsKJFS=-1");
            if (dr.Count() > 0)
            {
                DataTable dtKJFS = dr.CopyToDataTable();
                dtKJFS.Columns.Add("issel");
                List<string> UserKJFS = new JH_Auth_UserCustomDataB().GetEntities(d => d.ComId == UserInfo.User.ComId && d.UserName == UserInfo.User.UserName && d.DataType == "PCKJFS" && d.DataContent1 == "Y").Select(d => d.DataContent).ToList();
                foreach (DataRow row in dtKJFS.Rows)
                {
                    row["issel"] = UserKJFS.Contains(row["ModelCode"].ToString());
                }
                msg.Result1 = dtKJFS;
            }

        }
        #endregion









        #region 初始化模块类型数据
        public void INITMODELTYPE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            if (UserInfo.User.isSupAdmin != "Y")
            {
                return;
            }
            string strSql = "";
            string strSql1 = "";
            int classID = 0;
            //获取未初始化的应用code
            List<string> FormCodes = new JH_Auth_QY_ModelB().GetEntities(d => d.ComId == UserInfo.User.ComId.Value && d.IsInit != 1).Select(d => d.QYModelCode).ToList();
            foreach (string strCode in FormCodes)
            {
                strSql1 += string.Format("update JH_Auth_QY_Model set IsInit=1 where ComId={0} and QYModelCode='{1}'", UserInfo.User.ComId, strCode);
                switch (strCode)
                {
                    case "XXFB":
                        strSql += string.Format(@" if  not exists (SELECT * from SZHL_XXFBType where ComId=1 and TypeName='企业公告' and IsDel<>1 and PTypeID=0)
                                                BEGIN 
                                              INSERT INTO SZHL_XXFBType (ComId,TypeName,PTypeID,TypeDec,TypeManager,IsCheck,TypePath,CRUser,CRDate,IsDel) 
                                            values({0},'企业公告',0,'企业公告','{1}','False','','{1}',GETDATE(),0)
                                            DECLARE @pTypeId INT
                                            set @pTypeId=@@identity   
                                            INSERT INTO SZHL_XXFBType (ComId,TypeName,PTypeID,TypeDec,TypeManager,IsCheck,TypePath,CRUser,CRDate,IsDel) 
                                            values({0},'企业公告',@pTypeId,'企业公告','{1}','False',@pTypeId,'{1}',GETDATE(),0)  END", UserInfo.User.ComId, UserInfo.User.UserName);


                        break;
                    case "GZBG"://工作报告
                        string[] strGZBG = new string[] { "日报", "周报", "月报" };
                        classID = 6;
                        foreach (string str in strGZBG)
                        {
                            strSql += string.Format(@" if  not exists (SELECT * from JH_Auth_ZiDian where ComId={0} and TypeName='{1}' and Class={3}) INSERT into   JH_Auth_ZiDian(Class,TypeName,CRUser,CRDate,Remark,ComId) values({3},'{1}','{2}',GETDATE(),0,{0})", UserInfo.User.ComId, str, UserInfo.User.UserName, classID);
                        }
                        break;
                    case "CCXJ"://出差休假
                        string[] strCCXJ = new string[] { "事假", "病假", "婚假", "产假", "丧假" };
                        classID = 1;
                        foreach (string str in strCCXJ)
                        {
                            strSql += string.Format(@" if  not exists (SELECT * from JH_Auth_ZiDian where ComId={0} and TypeName='{1}' and Class={3}) INSERT into   JH_Auth_ZiDian(Class,TypeName,CRUser,CRDate,Remark,ComId) values({3},'{1}','{2}',GETDATE(),0,{0})", UserInfo.User.ComId, str, UserInfo.User.UserName, classID);
                        }
                        break;
                    case "YCGL"://用车管理
                        string[] strYCGL = new string[] { "轿车", "客车", "货车", "其它" };
                        classID = 5;
                        foreach (string str in strYCGL)
                        {
                            strSql += string.Format(@" if  not exists (SELECT * from JH_Auth_ZiDian where ComId={0} and TypeName='{1}' and Class={3}) INSERT into   JH_Auth_ZiDian(Class,TypeName,CRUser,CRDate,Remark,ComId) values({3},'{1}','{2}',GETDATE(),0,{0})", UserInfo.User.ComId, str, UserInfo.User.UserName, classID);
                        }
                        break;
                    case "JFBX"://经费报销
                        string[] strJFBX = new string[] { "交通", "住宿", "餐饮", "通讯", "补助", "办公", "其它" };
                        classID = 23;
                        foreach (string str in strJFBX)
                        {
                            strSql += string.Format(@" if  not exists (SELECT * from JH_Auth_ZiDian where ComId={0} and TypeName='{1}' and Class={3}) INSERT into   JH_Auth_ZiDian(Class,TypeName,CRUser,CRDate,Remark,ComId) values({3},'{1}','{2}',GETDATE(),0,{0})", UserInfo.User.ComId, str, UserInfo.User.UserName, classID);
                        }
                        break;
                    case "XMGL":
                        string[] strXMGL = new string[] { "普通项目", "重要项目" };
                        classID = 18;
                        foreach (string str in strXMGL)
                        {
                            strSql += string.Format(@" if  not exists (SELECT * from JH_Auth_ZiDian where ComId={0} and TypeName='{1}' and Class={3}) INSERT into   JH_Auth_ZiDian(Class,TypeName,CRUser,CRDate,Remark,ComId) values({3},'{1}','{2}',GETDATE(),0,{0})", UserInfo.User.ComId, str, UserInfo.User.UserName, classID);
                        }
                        break;
                    case "TSSQ":
                        string[] strTSSQ = new string[] { "经典分享", "综合讨论" };
                        classID = 19;
                        foreach (string str in strTSSQ)
                        {
                            strSql += string.Format(@" if  not exists (SELECT * from JH_Auth_ZiDian where ComId={0} and TypeName='{1}' and Class={3}) INSERT into   JH_Auth_ZiDian(Class,TypeName,CRUser,CRDate,Remark,ComId) values({3},'{1}','{2}',GETDATE(),0,{0})", UserInfo.User.ComId, str, UserInfo.User.UserName, classID);
                        }
                        break;

                    case "RWGL":
                        string[] strRWGL = new string[] { "一般任务", "重要任务" };
                        classID = 7;
                        foreach (string str in strRWGL)
                        {
                            strSql += string.Format(@" if  not exists (SELECT * from JH_Auth_ZiDian where ComId={0} and TypeName='{1}' and Class={3}) INSERT into   JH_Auth_ZiDian(Class,TypeName,CRUser,CRDate,Remark,ComId) values({3},'{1}','{2}',GETDATE(),0,{0})", UserInfo.User.ComId, str, UserInfo.User.UserName, classID);
                        }
                        break;
                    case "CRM":
                        #region 客户类型Class: 10
                        string[] strCRMKH = new string[] { "普通客户", "重要客户", "低价值客户", "其他客户" };
                        classID = 10;
                        foreach (string str in strCRMKH)
                        {
                            strSql += string.Format(@" if  not exists (SELECT * from JH_Auth_ZiDian where ComId={0} and TypeName='{1}' and Class={3}) INSERT into   JH_Auth_ZiDian(Class,TypeName,CRUser,CRDate,Remark,ComId) values({3},'{1}','{2}',GETDATE(),0,{0})", UserInfo.User.ComId, str, UserInfo.User.UserName, classID);
                        }
                        #endregion
                        #region 跟进状态Class: 11
                        string[] strCRMGJ = new string[] { "出访", "意向", "报价", "成交", "暂时搁置" };
                        classID = 11;
                        foreach (string str in strCRMGJ)
                        {
                            strSql += string.Format(@" if  not exists (SELECT * from JH_Auth_ZiDian where ComId={0} and TypeName='{1}' and Class={3}) INSERT into   JH_Auth_ZiDian(Class,TypeName,CRUser,CRDate,Remark,ComId) values({3},'{1}','{2}',GETDATE(),0,{0})", UserInfo.User.ComId, str, UserInfo.User.UserName, classID);
                        }
                        #endregion
                        #region 客户来源Class: 12
                        string[] strCRMLY = new string[] { "广告", "社交推广", "研讨会", "搜索引擎", "客户介绍", "独立研发", "代理商", "其他" };
                        classID = 12;
                        foreach (string str in strCRMLY)
                        {
                            strSql += string.Format(@" if  not exists (SELECT * from JH_Auth_ZiDian where ComId={0} and TypeName='{1}' and Class={3}) INSERT into   JH_Auth_ZiDian(Class,TypeName,CRUser,CRDate,Remark,ComId) values({3},'{1}','{2}',GETDATE(),0,{0})", UserInfo.User.ComId, str, UserInfo.User.UserName, classID);
                        }
                        #endregion
                        #region 所属行业Class: 13
                        string[] strCRMHY = new string[] { "金融", "服务", "电信", "教育", "高科技", "政府", "制造", "能源", "零食", "媒体", "娱乐", "咨询", "共用事业" };
                        classID = 13;
                        foreach (string str in strCRMHY)
                        {
                            strSql += string.Format(@" if  not exists (SELECT * from JH_Auth_ZiDian where ComId={0} and TypeName='{1}' and Class={3}) INSERT into   JH_Auth_ZiDian(Class,TypeName,CRUser,CRDate,Remark,ComId) values({3},'{1}','{2}',GETDATE(),0,{0})", UserInfo.User.ComId, str, UserInfo.User.UserName, classID);
                        }
                        #endregion
                        #region 人员规模Class: 14
                        string[] strCRMGM = new string[] { "小于10人", "20-50人", "50-100人", "100-500人", "500人以上" };
                        classID = 14;
                        foreach (string str in strCRMGM)
                        {
                            strSql += string.Format(@" if  not exists (SELECT * from JH_Auth_ZiDian where ComId={0} and TypeName='{1}' and Class={3}) INSERT into   JH_Auth_ZiDian(Class,TypeName,CRUser,CRDate,Remark,ComId) values({3},'{1}','{2}',GETDATE(),0,{0})", UserInfo.User.ComId, str, UserInfo.User.UserName, classID);
                        }
                        #endregion
                        #region 合同分类Class: 16
                        string[] strCRMHT = new string[] { "直销合同", "代理合同", "采购合同", "服务合同", "其他" };
                        classID = 16;
                        foreach (string str in strCRMHT)
                        {
                            strSql += string.Format(@" if  not exists (SELECT * from JH_Auth_ZiDian where ComId={0} and TypeName='{1}' and Class={3}) INSERT into   JH_Auth_ZiDian(Class,TypeName,CRUser,CRDate,Remark,ComId) values({3},'{1}','{2}',GETDATE(),0,{0})", UserInfo.User.ComId, str, UserInfo.User.UserName, classID);
                        }
                        #endregion
                        #region 付款方式Class: 17
                        string[] strCRMFK = new string[] { "银行转账", "现金", "支票", "其他" };
                        classID = 17;
                        foreach (string str in strCRMFK)
                        {
                            strSql += string.Format(@" if  not exists (SELECT * from JH_Auth_ZiDian where ComId={0} and TypeName='{1}' and Class={3}) INSERT into   JH_Auth_ZiDian(Class,TypeName,CRUser,CRDate,Remark,ComId) values({3},'{1}','{2}',GETDATE(),0,{0})", UserInfo.User.ComId, str, UserInfo.User.UserName, classID);
                        }

                        #endregion
                        break;
                    case "KDDY":
                        //strSql += "if  not exists (SELECT * from SZHL_KDDY_PZ where ComId=" + UserInfo.User.ComId + " and KDName='圆通') INSERT INTO [SZHL_KDDY_PZ] ([ComId], [KDName],[imageWidth],[imageHeight],[objects],[FontSize],[Font],[Horizontal],[Vertical],[KDImg],[CRUser],[CRDate]) VALUES (" + UserInfo.User.ComId + ", '圆通', '255', '149', '[{\"name\":\"sendUser\",\"text\":\"发件人姓名 \",\"w\":\"102\",\"h\":\"30\",\"top\":\"92\",\"left\":\"130\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"sendTel\",\"text\":\"发件人手机 \",\"w\":\"124\",\"h\":\"30\",\"top\":\"90\",\"left\":\"290\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"sendCompany\",\"text\":\"发件人公司 \",\"w\":\"177\",\"h\":\"30\",\"top\":\"120\",\"left\":\"142\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"sendAddress\",\"text\":\"发件人地址 \",\"w\":\"336\",\"h\":\"30\",\"top\":\"151\",\"left\":\"125\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"receiverUser\",\"text\":\"收件人姓名 \",\"w\":\"102\",\"h\":\"30\",\"top\":\"206\",\"left\":\"130\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"receiverTel\",\"text\":\"收件人手机 \",\"w\":\"172\",\"h\":\"30\",\"top\":\"206\",\"left\":\"290\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"receiverCompany\",\"text\":\"收件人公司 \",\"w\":\"180\",\"h\":\"30\",\"top\":\"236\",\"left\":\"139\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"receiverAddress\",\"text\":\"收件人地址 \",\"w\":\"341\",\"h\":\"32\",\"top\":\"267\",\"left\":\"123\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"}]', '16px', '宋体', '0', '0', '2612', '" + UserInfo.User.UserName + "', GETDATE())";
                        //strSql += "if  not exists (SELECT * from SZHL_KDDY_PZ where ComId=" + UserInfo.User.ComId + " and KDName='顺丰') INSERT INTO [SZHL_KDDY_PZ] ([ComId], [KDName],[imageWidth],[imageHeight],[objects],[FontSize],[Font],[Horizontal],[Vertical],[KDImg],[CRUser],[CRDate]) VALUES (" + UserInfo.User.ComId + ", '顺丰', '215', '140', '[{\"name\":\"sendUser\",\"text\":\"发件人姓名 \",\"w\":\"102\",\"h\":\"30\",\"top\":\"144\",\"left\":\"302\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"sendTel\",\"text\":\"发件人手机 \",\"w\":\"135\",\"h\":\"30\",\"top\":\"224\",\"left\":\"184\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"sendCompany\",\"text\":\"发件人公司 \",\"w\":\"148\",\"h\":\"30\",\"top\":\"144\",\"left\":\"119\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"sendAddress\",\"text\":\"发件人地址 \",\"w\":\"266\",\"h\":\"49\",\"top\":\"174\",\"left\":\"109\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"receiverUser\",\"text\":\"收件人姓名 \",\"w\":\"102\",\"h\":\"30\",\"top\":\"290\",\"left\":\"303\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"receiverTel\",\"text\":\"收件人手机 \",\"w\":\"191\",\"h\":\"31\",\"top\":\"372\",\"left\":\"187\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"receiverCompany\",\"text\":\"收件人公司 \",\"w\":\"152\",\"h\":\"30\",\"top\":\"290\",\"left\":\"116\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"receiverAddress\",\"text\":\"收件人地址 \",\"w\":\"271\",\"h\":\"48\",\"top\":\"322\",\"left\":\"108\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"}]', '16px', '宋体', '0', '0', '2615', '" + UserInfo.User.UserName + "', GETDATE())";
                        //strSql += "if  not exists (SELECT * from SZHL_KDDY_PZ where ComId=" + UserInfo.User.ComId + " and KDName='EMS')  INSERT INTO [SZHL_KDDY_PZ] ([ComId], [KDName],[imageWidth],[imageHeight],[objects],[FontSize],[Font],[Horizontal],[Vertical],[KDImg],[CRUser],[CRDate]) VALUES (" + UserInfo.User.ComId + ", 'EMS',  '230', '127', '[{\"name\":\"sendUser\",\"text\":\"发件人姓名 \",\"w\":\"102\",\"h\":\"30\",\"top\":\"92\",\"left\":\"130\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"sendTel\",\"text\":\"发件人手机 \",\"w\":\"124\",\"h\":\"30\",\"top\":\"90\",\"left\":\"290\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"sendCompany\",\"text\":\"发件人公司 \",\"w\":\"177\",\"h\":\"30\",\"top\":\"120\",\"left\":\"142\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"sendAddress\",\"text\":\"发件人地址 \",\"w\":\"336\",\"h\":\"30\",\"top\":\"151\",\"left\":\"125\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"receiverUser\",\"text\":\"收件人姓名 \",\"w\":\"102\",\"h\":\"30\",\"top\":\"206\",\"left\":\"130\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"receiverTel\",\"text\":\"收件人手机 \",\"w\":\"172\",\"h\":\"30\",\"top\":\"206\",\"left\":\"290\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"receiverCompany\",\"text\":\"收件人公司 \",\"w\":\"180\",\"h\":\"30\",\"top\":\"236\",\"left\":\"139\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"},{\"name\":\"receiverAddress\",\"text\":\"收件人地址 \",\"w\":\"341\",\"h\":\"32\",\"top\":\"267\",\"left\":\"123\",\"font_size\":\"16px\",\"font_weight\":\"400\",\"font_family\":\"宋体\"}]', '16px', '宋体', '0', '0', '2616', '" + UserInfo.User.UserName + "', GETDATE())";
                        strSql += "if  not exists (SELECT * from SZHL_KDDY_PZ where ComId=" + UserInfo.User.ComId + " and KDName='圆通') INSERT INTO [SZHL_KDDY_PZ] ([ComId], [KDName],[imageWidth],[imageHeight],[objects],[FontSize],[Font],[Horizontal],[Vertical],[KDImg],[CRUser],[CRDate]) select " + UserInfo.User.ComId + ",KDName, imageWidth, imageHeight, objects, FontSize, Font, Horizontal, Vertical, KDImg, '" + UserInfo.User.UserName + "', GETDATE() from dbo.SZHL_KDDY_PZ where ComId=0 and KDName='圆通'";
                        strSql += "if  not exists (SELECT * from SZHL_KDDY_PZ where ComId=" + UserInfo.User.ComId + " and KDName='顺丰') INSERT INTO [SZHL_KDDY_PZ] ([ComId], [KDName],[imageWidth],[imageHeight],[objects],[FontSize],[Font],[Horizontal],[Vertical],[KDImg],[CRUser],[CRDate]) select " + UserInfo.User.ComId + ",KDName, imageWidth, imageHeight, objects, FontSize, Font, Horizontal, Vertical, KDImg, '" + UserInfo.User.UserName + "', GETDATE() from dbo.SZHL_KDDY_PZ where ComId=0 and KDName='顺丰'";
                        strSql += "if  not exists (SELECT * from SZHL_KDDY_PZ where ComId=" + UserInfo.User.ComId + " and KDName='EMS') INSERT INTO [SZHL_KDDY_PZ] ([ComId], [KDName],[imageWidth],[imageHeight],[objects],[FontSize],[Font],[Horizontal],[Vertical],[KDImg],[CRUser],[CRDate]) select " + UserInfo.User.ComId + ",KDName, imageWidth, imageHeight, objects, FontSize, Font, Horizontal, Vertical, KDImg, '" + UserInfo.User.UserName + "', GETDATE() from dbo.SZHL_KDDY_PZ where ComId=0 and KDName='EMS'";
                        break;
                }
            }
            if (!string.IsNullOrEmpty(strSql))
            {
                new JH_Auth_ZiDianB().ExsSql(strSql);
            }
            if (!string.IsNullOrEmpty(strSql1))
            {
                new JH_Auth_QY_ModelB().ExsSql(strSql1);
            }
            JH_Auth_QY qymodel = UserInfo.QYinfo;
            qymodel.SystemGGId = "Y";
            new JH_Auth_QYB().Update(qymodel);

        }
        #endregion

        #region EXCELTODatatable
        public void IMPORTUSER(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {

            HttpPostedFile _upfile = context.Request.Files["upFile"];

            string headrow = context.Request["headrow"] ?? "0";//头部开始行下标
            if (_upfile == null)
            {
                msg.ErrorMsg = "请选择要上传的文件 ";
            }
            try
            {
                msg.Result = new CommonHelp().ExcelToTable(_upfile, int.Parse(headrow));
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }
        #endregion
        #region 导入用户
        public void SAVEIMPORTUSER(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string branchMsg = "", branchErrorMsg = "", userMsg = "";
            int i = 0, j = 0;
            DataTable dt = new DataTable();
            dt = JsonConvert.DeserializeObject<DataTable>(P1);
            dt.Columns.Add("BranchCode");
            JH_Auth_Branch branchroot = new JH_Auth_BranchB().GetEntity(d => d.ComId == UserInfo.User.ComId && d.DeptRoot == -1);

            int rowIndex = 0;
            foreach (DataRow row in dt.Rows)
            {
                int bRootid = branchroot.DeptCode;
                rowIndex++;
                string branchName = row[4].ToString();
                if (branchName != "")
                {
                    string[] branchNames = branchName.Split('/');
                    for (int l = 0; l < branchNames.Length; l++)
                    {
                        string strBranch = branchNames[l];
                        JH_Auth_Branch branchModel = new JH_Auth_BranchB().GetEntity(d => d.DeptName == strBranch && d.ComId == UserInfo.User.ComId);
                        if (branchModel != null)
                        {
                            bRootid = branchModel.DeptCode;
                            if (l == branchNames.Length - 1)
                            {
                                row["BranchCode"] = branchModel.DeptCode;
                            }
                        }
                        else
                        {
                            branchModel = new JH_Auth_Branch();
                            branchModel.DeptName = strBranch;
                            branchModel.DeptDesc = strBranch;
                            branchModel.ComId = UserInfo.User.ComId;
                            branchModel.DeptRoot = bRootid;
                            branchModel.Remark1 = new JH_Auth_BranchB().GetBranchNo(UserInfo.User.ComId.Value, bRootid);
                            branchModel.CRDate = DateTime.Now;
                            branchModel.CRUser = UserInfo.User.UserName;
                            new JH_Auth_BranchB().Insert(branchModel);
                            try
                            {
                                bRootid = branchModel.DeptCode;
                                if (l == branchNames.Length - 1)
                                {
                                    row["BranchCode"] = branchModel.DeptCode;
                                }
                                i++;
                                branchMsg += "新增部门“" + strBranch + "”成功<br/>";
                            }
                            catch (Exception ex)
                            {

                                branchErrorMsg += "部门：" + strBranch + "失败 " + msg.ErrorMsg + "<br/>";
                            }
                        }
                    }
                    string userName = row[2].ToString();
                    JH_Auth_User userModel = new JH_Auth_UserB().GetEntity(d => d.UserName == userName && d.ComId == UserInfo.User.ComId);
                    if (userModel == null)
                    {
                        JH_Auth_User userNew = new JH_Auth_User();
                        userNew.BranchCode = int.Parse(row["BranchCode"].ToString());
                        userNew.ComId = UserInfo.User.ComId;
                        userNew.IsUse = "Y";
                        userNew.mailbox = row[3].ToString();
                        userNew.mobphone = row[2].ToString();
                        userNew.RoomCode = row[7].ToString();
                        userNew.Sex = row[1].ToString();
                        userNew.telphone = row[9].ToString();

                        string s = "2050-01-01 18:38:50";
                        DateTime result;
                        if (DateTime.TryParse(row[10].ToString(), out result))
                        {
                            userNew.Birthday = result;
                        }
                        
                        userNew.UserGW = row[6].ToString();
                        userNew.UserName = row[2].ToString();
                        userNew.UserRealName = row[0].ToString();
                        userNew.zhiwu = row[5].ToString() == "" ? "员工" : row[5].ToString();
                        userNew.UserPass = CommonHelp.GetMD5(P2);
                        userNew.CRDate = DateTime.Now;
                        userNew.CRUser = UserInfo.User.UserName;

                        if (!string.IsNullOrEmpty(row[8].ToString()))
                        {
                            int orderNum = 0;
                            int.TryParse(row[8].ToString(), out orderNum);
                            userNew.UserOrder = orderNum;

                        }
                        try
                        {
                            msg.ErrorMsg = "";
                            if (string.IsNullOrEmpty(userNew.UserName))
                            {
                                msg.ErrorMsg = "用户名必填";
                            }
                            //Regex regexPhone = new Regex("^0?1[3|4|5|8|7][0-9]\\d{8}$");
                            //if (!regexPhone.IsMatch(userNew.UserName))
                            //{
                            //    msg.ErrorMsg = "用户名必须为手机号";
                            //}
                            if (string.IsNullOrEmpty(userNew.mobphone))
                            {
                                msg.ErrorMsg = "手机号必填";
                            }
                            //if (!regexPhone.IsMatch(userNew.mobphone))
                            //{
                            //    msg.ErrorMsg = "手机号填写不正确";
                            //}
                            Regex regexOrder = new Regex("^[0-9]*$");
                            if (userNew.UserOrder != null && !regexOrder.IsMatch(userNew.UserOrder.ToString()))
                            {
                                msg.ErrorMsg = "序号必须是数字";
                            }
                            if (msg.ErrorMsg != "")
                            {
                                userMsg += "第" + rowIndex + "行" + msg.ErrorMsg + "<br/>";
                            }
                            if (msg.ErrorMsg == "")
                            {
                                new JH_Auth_UserB().Insert(userNew);
                                JH_Auth_Role role = new JH_Auth_RoleB().GetEntity(d => d.RoleName == userNew.zhiwu && d.ComId == UserInfo.User.ComId);
                                if (role == null)
                                {
                                    role = new JH_Auth_Role();
                                    role.PRoleCode = 0;
                                    role.RoleName = userNew.zhiwu;
                                    role.RoleDec = userNew.zhiwu;
                                    role.IsUse = "Y";
                                    role.isSysRole = "N";
                                    role.leve = 0;
                                    role.ComId = UserInfo.User.ComId;
                                    role.DisplayOrder = 0;
                                    new JH_Auth_RoleB().Insert(role);
                                }
                                string strSql = string.Format("INSERT into JH_Auth_UserRole (UserName,RoleCode,ComId) Values('{0}',{1},{2})", userNew.UserName, role.RoleCode, UserInfo.User.ComId);
                                new JH_Auth_RoleB().ExsSql(strSql);
                                string isFS = context.Request["issend"] ?? "";
                                if (isFS.ToLower() == "true")
                                {
                                    string content = string.Format("尊敬的" + userNew.UserName + "用户您好：你已被添加到" + UserInfo.QYinfo.QYName + ",账号：" + userNew.mobphone + "，密码" + P2 + ",登录请访问" + UserInfo.QYinfo.WXUrl);
                                   new SZHL_DXGLB().SendSMS(userNew.mobphone, content, userNew.ComId.Value);
                                }
                                j++;
                            }
                        }
                        catch (Exception ex)
                        {
                            userMsg += "第" + rowIndex + "行" + msg.ErrorMsg + "<br/>";
                        }

                    }
                    else
                    {

                        userMsg += "第" + rowIndex + "行" + "用户“" + row[2].ToString() + "”已存在<br/>";
                    }
                }
                else
                {
                    branchErrorMsg += "第" + rowIndex + "行所在部门必填<br/>";
                }

            }
            msg.Result = branchErrorMsg + "<br/>" + userMsg;
            msg.Result1 = "新增部门" + i + "个,新增用户" + j + "个<br/>" + branchMsg + (branchMsg == "" ? "" : "<br/>");
        }


        /// <summary>
        /// 将系统的组织架构同步到微信中去
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void TBBRANCHUSER(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {

            //判断是否启用微信后，启用部门需要同步添加微信部门
            if (UserInfo.QYinfo.IsUseWX == "Y")
            {

                #region 同步部门

                //系统部门
                List<JH_Auth_Branch> branchList = new JH_Auth_BranchB().GetEntities(d => d.ComId == UserInfo.User.ComId && d.WXBMCode == null).ToList();

                WXHelp wx = new WXHelp(UserInfo.QYinfo);
                //微信部门
                Senparc.Weixin.QY.AdvancedAPIs.MailList.GetDepartmentListResult bmlist = wx.WX_GetBranchList("");
                foreach (JH_Auth_Branch branch in branchList)
                {
                    List<DepartmentList> departList = bmlist.department.Where(d => d.name == branch.DeptName).ToList();
                    QyJsonResult result = null;
                    if (departList.Count() > 0)
                    {
                        branch.WXBMCode = departList[0].id;
                        result = wx.WX_UpdateBranch(branch);
                    }
                    else
                    {

                        int branchWxCode = wx.WX_CreateBranchTB(branch);
                        branch.WXBMCode = branchWxCode;
                    }
                    new JH_Auth_BranchB().Update(branch);
                }

                #endregion

                #region 同步人员
                JH_Auth_Branch branchModel = new JH_Auth_BranchB().GetEntity(d => d.DeptRoot == -1 && d.ComId == UserInfo.User.ComId);

                Senparc.Weixin.QY.AdvancedAPIs.MailList.GetDepartmentMemberInfoResult yg = wx.WX_GetDepartmentMemberInfo(branchModel.WXBMCode.Value);
                List<JH_Auth_User> userList = new JH_Auth_UserB().GetEntities(d => d.ComId == UserInfo.User.ComId && d.UserName != "administrator").ToList();
                foreach (JH_Auth_User user in userList)
                {
                    if (yg.userlist.Where(d => d.name == user.UserName || d.mobile == user.mobphone).Count() > 0)
                    {
                        wx.WX_UpdateUser(user);
                    }
                    else
                    {

                        wx.WX_CreateUser(user);
                    }
                }
                #endregion
            }
        }
        public void SAVEIMPORTUSERBAK(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string branchMsg = "", userMsg = "";
            int i = 0, j = 0;
            DataTable dt = new DataTable();
            dt = JsonConvert.DeserializeObject<DataTable>(P1);
            dt.Columns.Add("BranchCode");
            JH_Auth_Branch branchroot = new JH_Auth_BranchB().GetEntity(d => d.ComId == UserInfo.User.ComId && d.DeptRoot == -1);

            int rowIndex = 0;
            foreach (DataRow row in dt.Rows)
            {
                int bRootid = branchroot.DeptCode;
                rowIndex++;
                string branchName = row[4].ToString();
                if (branchName != "")
                {
                    string[] branchNames = branchName.Split('/');
                    for (int l = 0; l < branchNames.Length; l++)
                    {
                        string strBranch = branchNames[l];
                        JH_Auth_Branch branchModel = new JH_Auth_BranchB().GetEntity(d => d.DeptName == strBranch && d.ComId == UserInfo.User.ComId);
                        if (branchModel != null)
                        {
                            bRootid = branchModel.DeptCode;
                            if (l == branchNames.Length - 1)
                            {
                                row["BranchCode"] = branchModel.DeptCode;
                            }
                        }
                        else
                        {
                            branchModel = new JH_Auth_Branch();
                            branchModel.DeptName = strBranch;
                            branchModel.DeptDesc = strBranch;
                            branchModel.DeptRoot = bRootid;
                            new JH_Auth_BranchB().AddBranch(UserInfo, branchModel, msg);
                            if (msg.ErrorMsg == "")
                            {
                                bRootid = ((JH_Auth_Branch)msg.Result).DeptCode;
                                if (l == branchNames.Length - 1)
                                {
                                    row["BranchCode"] = branchModel.DeptCode;
                                }
                                i++;
                                branchMsg += "新增部门“" + strBranch + "”成功<br/>";
                            }
                            else
                            {
                                branchMsg += "部门：" + strBranch + " " + msg.ErrorMsg;
                            }
                        }
                    }
                    string userName = row[2].ToString();
                    JH_Auth_User userModel = new JH_Auth_UserB().GetEntity(d => d.UserName == userName && d.ComId == UserInfo.User.ComId);
                    if (userModel == null)
                    {
                        JH_Auth_User userNew = new JH_Auth_User();
                        userNew.BranchCode = int.Parse(row["BranchCode"].ToString());
                        userNew.ComId = UserInfo.User.ComId;
                        userNew.IsUse = "Y";
                        userNew.mailbox = row[3].ToString();
                        userNew.mobphone = row[2].ToString();
                        userNew.RoomCode = row[7].ToString();
                        userNew.Sex = row[1].ToString();
                        userNew.telphone = row[9].ToString();
                        userNew.UserGW = row[6].ToString();
                        userNew.UserName = row[2].ToString();
                        userNew.UserRealName = row[0].ToString();
                        userNew.zhiwu = row[5].ToString();
                        userNew.UserPass = CommonHelp.GetMD5(P2);
                        if (!string.IsNullOrEmpty(row[8].ToString()))
                        {
                            int orderNum = 0;
                            int.TryParse(row[8].ToString(), out orderNum);
                            userNew.UserOrder = orderNum;

                        }
                        new AuthManage().ADDUSER(context, msg, JsonConvert.SerializeObject(userNew), "", UserInfo);
                        if (msg.ErrorMsg != "")
                        {
                            userMsg += "第" + rowIndex + "行" + msg.ErrorMsg + "<br/>";
                        }
                        else
                        {
                            j++;
                        }

                    }
                    else
                    {

                        userMsg += "第" + rowIndex + "行" + "用户“" + row[2].ToString() + "”已存在<br/>";
                    }
                }
                else
                {
                    branchMsg += "第" + rowIndex + "行部门必填";
                }

            }
            msg.Result = branchMsg + (branchMsg == "" ? "" : "<br/>") + userMsg;
            msg.Result1 = "新增部门" + i + "个,新增用户" + j + "个<br/>";

        }

        public void TBTXL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {

                int bmcount = 0;
                int rycount = 0;
                if (P1 == "")
                {
                    msg.ErrorMsg = "请输入初始密码";
                    return;
                }
                WXHelp wx = new WXHelp(UserInfo.QYinfo);
                #region 更新部门
                Senparc.Weixin.QY.AdvancedAPIs.MailList.GetDepartmentListResult bmlist = wx.WX_GetBranchList("");
                foreach (var wxbm in bmlist.department.OrderBy(d => d.parentid))
                {
                    var bm = new JH_Auth_BranchB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.WXBMCode == wxbm.id);
                    if (bm == null)
                    {
                        #region 新增部门
                        JH_Auth_Branch jab = new JH_Auth_Branch();
                        jab.WXBMCode = wxbm.id;
                        jab.ComId = UserInfo.User.ComId;
                        jab.DeptName = wxbm.name;
                        jab.DeptDesc = wxbm.name;
                        jab.DeptShort = wxbm.order;

                        if (wxbm.parentid == 0)//如果是跟部门,设置其跟部门为-1
                        {
                            jab.DeptRoot = -1;
                        }
                        else
                        {
                            var bm1 = new JH_Auth_BranchB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.WXBMCode == wxbm.parentid);
                            jab.DeptRoot = bm1.DeptCode;
                            jab.Remark1 = new JH_Auth_BranchB().GetBranchNo(UserInfo.User.ComId.Value, jab.DeptRoot);
                        }


                        new JH_Auth_BranchB().Insert(jab);

                        bmcount = bmcount + 1;
                        #endregion
                    }
                    else
                    {
                        //同步部门时放弃更新现有部门

                    }
                }
                #endregion

                #region 更新人员
                JH_Auth_Branch branchModel = new JH_Auth_BranchB().GetEntity(d => d.DeptRoot == -1 && d.ComId == UserInfo.User.ComId);

                Senparc.Weixin.QY.AdvancedAPIs.MailList.GetDepartmentMemberInfoResult yg = wx.WX_GetDepartmentMemberInfo(branchModel.WXBMCode.Value);
                foreach (var u in yg.userlist)
                {
                    var user = new JH_Auth_UserB().GetUserByUserName(UserInfo.QYinfo.ComId, u.userid);
                    if (user == null)
                    {
                        #region 新增人员
                        JH_Auth_User jau = new JH_Auth_User();
                        jau.ComId = UserInfo.User.ComId;
                        jau.UserName = u.userid;
                        jau.UserPass = CommonHelp.GetMD5(P1);
                        jau.UserRealName = u.name;
                        jau.Sex = u.gender == 1 ? "男" : "女";
                        if (u.department.Length > 0)
                        {
                            int id = u.department[0];
                            var bm1 = new JH_Auth_BranchB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.WXBMCode == id);
                            jau.BranchCode = bm1.DeptCode;
                        }
                        jau.mailbox = u.email;
                        jau.mobphone = u.mobile;
                        jau.weixinnum = u.weixinid;
                        jau.zhiwu = string.IsNullOrEmpty(u.position) ? "员工" : u.position;
                        jau.IsUse = "Y";

                        if (u.status == 1 || u.status == 4)
                        {
                            jau.isgz = u.status.ToString();
                        }
                        jau.txurl = u.avatar;

                        new JH_Auth_UserB().Insert(jau);

                        rycount = rycount + 1;
                        #endregion

                        //为所有人增加普通员工的权限
                        JH_Auth_Role rdefault = new JH_Auth_RoleB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.isSysRole == "Y" && p.RoleName == "员工");//找到默认角色
                        if (rdefault != null)
                        {
                            JH_Auth_UserRole jaurdefault = new JH_Auth_UserRole();
                            jaurdefault.ComId = UserInfo.User.ComId;
                            jaurdefault.RoleCode = rdefault.RoleCode;
                            jaurdefault.UserName = jau.UserName;
                            new JH_Auth_UserRoleB().Insert(jaurdefault);
                        }


                    }
                    else
                    {
                        //同步人员时放弃更新现有人员
                        #region 更新人员
                        user.UserRealName = u.name;
                        if (u.department.Length > 0)
                        {
                            int id = u.department[0];
                            var bm1 = new JH_Auth_BranchB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.WXBMCode == id);
                            user.BranchCode = bm1.DeptCode;
                        }
                        user.mailbox = u.email;
                        user.mobphone = u.mobile;
                        user.weixinnum = u.weixinid;
                        user.zhiwu = string.IsNullOrEmpty(u.position) ? "员工" : u.position;
                        user.Sex = u.gender == 1 ? "男" : "女";
                        if (u.status == 1 || u.status == 4)
                        {
                            user.IsUse = "Y";
                            user.isgz = u.status.ToString();
                        }
                        else if (u.status == 2)
                        {
                            user.IsUse = "N";
                        }
                        user.txurl = u.avatar;

                        new JH_Auth_UserB().Update(user);
                        #endregion
                    }

                    #region 更新角色(职务)
                    if (!string.IsNullOrEmpty(u.position))
                    {
                        var r = new JH_Auth_RoleB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.RoleName == u.position);

                        if (r == null)
                        {
                            JH_Auth_Role jar = new JH_Auth_Role();
                            jar.ComId = UserInfo.User.ComId;
                            jar.RoleName = u.position;
                            jar.RoleDec = u.position;
                            jar.PRoleCode = 0;
                            jar.isSysRole = "N";
                            jar.IsUse = "Y";
                            jar.leve = 0;
                            jar.DisplayOrder = 0;

                            new JH_Auth_RoleB().Insert(jar);

                            JH_Auth_UserRole jaur = new JH_Auth_UserRole();
                            jaur.ComId = UserInfo.User.ComId;
                            jaur.RoleCode = jar.RoleCode;
                            jaur.UserName = u.userid;
                            new JH_Auth_UserRoleB().Insert(jaur);


                        }
                        else
                        {
                            //同步人员时放弃更新现有职务
                            //var ur = new JH_Auth_UserRoleB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.UserName == u.userid && p.RoleCode == r.RoleCode);
                            //if (ur == null)
                            //{
                            //    JH_Auth_UserRole jaur = new JH_Auth_UserRole();
                            //    jaur.ComId = UserInfo.User.ComId;
                            //    jaur.RoleCode = r.RoleCode;
                            //    jaur.UserName = u.userid;

                            //    new JH_Auth_UserRoleB().Insert(jaur);
                            //}
                        }
                    }
                    #endregion
                }
                #endregion

                #region 更新标签
                //Senparc.Weixin.QY.AdvancedAPIs.MailList.GetTagListResult tags = wx.WX_GetTagList();
                //foreach (var t in tags.taglist)
                //{
                //    int tid = Int32.Parse(t.tagid);

                //    string us = string.Empty;
                //    Senparc.Weixin.QY.AdvancedAPIs.MailList.GetTagMemberResult bqry = wx.WX_GetTagMember(tid);

                //    foreach (var u in bqry.userlist)
                //    {
                //        if (string.IsNullOrEmpty(us))
                //        {
                //            us = u.userid;
                //        }
                //        else
                //        {
                //            us = us + "," + u.userid;
                //        }
                //    }

                //    var ts = new JH_Auth_UserCustomDataB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.WXBQCode == tid);
                //    if (ts == null)
                //    {
                //        JH_Auth_UserCustomData jar = new JH_Auth_UserCustomData();
                //        jar.ComId = UserInfo.User.ComId;
                //        jar.WXBQCode = tid;
                //        jar.DataType = "USERGROUP";
                //        jar.DataContent = t.tagname;
                //        jar.DataContent1 = us;

                //        new JH_Auth_UserCustomDataB().Insert(jar);
                //    }
                //    else
                //    {
                //        ts.DataContent = t.tagname;
                //        ts.DataContent1 = us;
                //        new JH_Auth_UserCustomDataB().Update(ts);
                //    }
                //}

                #endregion

                msg.Result1 = bmcount;
                msg.Result2 = rycount;


            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.ToString();
            }
        }

        //同步关注状态
        public void TBGZSTATUS(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {

                JH_Auth_Branch branchModel = new JH_Auth_BranchB().GetEntity(d => d.DeptRoot == -1 && d.ComId == UserInfo.User.ComId);

                #region 同步用户关注状态
                WXHelp wx = new WXHelp(UserInfo.QYinfo);
                GetDepartmentMemberInfoResult yg = wx.WX_GetDepartmentMemberInfo(branchModel.WXBMCode.Value);

                if (yg != null && yg.userlist != null)
                {
                    foreach (var u in yg.userlist)
                    {

                        JH_Auth_User user = new JH_Auth_UserB().GetEntity(d => d.ComId == UserInfo.User.ComId && d.UserName == u.userid);

                        if (user != null && u != null && (u.status == 1 || u.status == 4))
                        {
                            user.isgz = u.status.ToString();
                            new JH_Auth_UserB().Update(user);
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
                #endregion

        }
        #endregion

        #region 获取系统首页用户数量信息
        public void GETUSERCOUNT(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strSql = string.Format("SELECT COUNT(0) TotalUser,isnull(sum(case when isgz=1 then 1 else 0 end ),0) gzCount,isnull(sum(case when isgz=4 then 1 else 0 end ),0) wgzCount,isnull(sum(case when IsUse!='Y' then 1 else 0 end ),0) wjhCount from JH_Auth_User where ComId={0}", UserInfo.User.ComId);
            msg.Result = new JH_Auth_UserB().GetDTByCommand(strSql);
            msg.Result1 = UserInfo.QYinfo.IsUseWX;
        }
        #endregion

        /// <summary>
        /// 同步通讯录
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">初始化密码</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>


        //更改管理员手机号
        public void CHANGEADMIN(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            List<JH_Auth_User> userList = new JH_Auth_UserB().GetEntities(d => (d.mobphone == P1 || d.UserName == P1) && d.isSupAdmin == "Y").ToList();
            if (userList.Count() > 0)
            {
                msg.ErrorMsg = "此手机已是超级管理员，请更换手机号";
            }
            else
            {
                JH_Auth_User userModel = UserInfo.User;
                userModel.UserName = P1;
                userModel.mobphone = P1;
                new JH_Auth_UserB().Update(userModel);
            }
        }



        #region 企业号相关


        public void YZCOMPANYQYH(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            JH_Auth_QY company = new JH_Auth_QY();
            company = JsonConvert.DeserializeObject<JH_Auth_QY>(P1);


            if (string.IsNullOrEmpty(company.corpSecret) || string.IsNullOrEmpty(company.corpId))
            {
                msg.ErrorMsg = "初始化企业号信息失败,corpId,corpSecret 不能为空";
                return;
            }
            if (!new JH_Auth_QYB().Update(company))
            {
                msg.ErrorMsg = "初始化企业号信息失败";
                return;
            }

        }




        /// <summary>
        /// 获取具有手机端的应用列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETWXAPP(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            msg.Result = new JH_Auth_ModelB().GetEntities(d => !string.IsNullOrEmpty(d.WXUrl)).OrderBy(d => d.ORDERID);
        }


        /// <summary>
        /// 获取当前企业号拥有的IP,只返回和可信域名相同的应用
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETQYAPP(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int id = Int32.Parse(P1);
            var model = new JH_Auth_ModelB().GetEntity(p => p.ID == id);
            msg.Result1 = model;//系统应用数据


            #region 获取应用默认菜单
            DataTable dt = new JH_Auth_CommonB().GetDTByCommand(" select * from JH_Auth_Common where ModelCode='" + model.ModelCode + "' and TopID='0' and type='1' order by Sort");
            dt.Columns.Add("Item", Type.GetType("System.Object"));
            foreach (DataRow dr in dt.Rows)
            {
                int tid = Int32.Parse(dr["ID"].ToString());
                dr["Item"] = new JH_Auth_CommonB().GetEntities(p => p.ModelCode == model.ModelCode && p.TopID == tid && p.Type == "1").OrderBy(p => p.Sort);
            }
            #endregion

            msg.Result2 = dt;

            //主页型应用的URL
            if (model.AppType == "2")
            {
                msg.Result3 = UserInfo.QYinfo.WXUrl.TrimEnd('/') + "/View_Mobile/UI/UI_COMMON.html?funcode=" + model.ModelCode + "&corpId=" + UserInfo.QYinfo.corpId; ;
            }
        }

        /// <summary>
        /// 保存应用Token和EncodingAESKey
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void SAVEMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            JH_Auth_Model model = JsonConvert.DeserializeObject<JH_Auth_Model>(P1);
            if (model.ID != 0)
            {
                if (string.IsNullOrEmpty(model.AppID))
                {
                    msg.ErrorMsg = "至少选择一个企业号应用才能绑定";
                    return;
                }

                if (model.AppType == "1" && (string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.EncodingAESKey)))
                {
                    msg.ErrorMsg = "Token、EncodingAESKey、企业号应用不能为空";
                }
                else
                {
                    new JH_Auth_ModelB().Update(model);
                }
            }
            else
            {
                msg.ErrorMsg = "绑定失败";
            }
        }

        /// <summary>
        /// 创建应用菜单
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void CREATEMENU(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int id = Int32.Parse(P1);
                var model = new JH_Auth_ModelB().GetEntity(p => p.ID == id);
                if (model != null)
                {
                    if (string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.EncodingAESKey) || string.IsNullOrEmpty(model.AppID))
                    {
                        msg.ErrorMsg = "Token、EncodingAESKey、企业号应用不能为空";
                    }
                    else
                    {
                        WXHelp WX = new WXHelp(UserInfo.QYinfo);
                        List<Senparc.Weixin.QY.Entities.Menu.BaseButton> lm = new List<Senparc.Weixin.QY.Entities.Menu.BaseButton>();
                        QyJsonResult rel = WX.WX_WxCreateMenuNew(Int32.Parse(model.AppID), model.ModelCode, ref lm);
                        if (rel.errmsg != "ok")
                        {
                            msg.ErrorMsg = "创建菜单失败";
                        }
                    }
                }
                else
                {
                    msg.ErrorMsg = "当前应用不存在";
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = "创建菜单失败";
            }
        }

        /// <summary>
        /// 解除应用绑定
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void FIREMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int id = Int32.Parse(P1);
                var model = new JH_Auth_ModelB().GetEntity(p => p.ID == id);
                if (model != null)
                {
                    //WXHelp WX = new WXHelp(UserInfo.QYinfo);
                    //WX.WX_DelMenu(Int32.Parse( model.AppID));

                    model.AppID = "";
                    model.Token = "";
                    model.EncodingAESKey = "";

                    new JH_Auth_ModelB().Update(model);
                }
                else
                {
                    msg.ErrorMsg = "当前应用不存在";
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = "解除绑定失败";
            }
        }

        

        #endregion
    }
}
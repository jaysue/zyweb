using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using QJY.Data;
using System.Text;
using Newtonsoft.Json;
using System.Web.UI.WebControls;
using System.Threading.Tasks;
using System.Collections;
using System.Linq.Expressions;
using QJY.Common;

namespace QJY.API
{
    #region 系统模块
    //用户表
    public class JH_Auth_UserB : BaseEFDao<JH_Auth_User>
    {



        public class UserInfo
        {
            public JH_Auth_User User;
            public JH_Auth_QY QYinfo;
            public JH_Auth_Branch BranchInfo;
            public string UserRoleCode;
            public string UserBMQXCode;

        }
        public UserInfo GetUserInfo(string strSZHLCode)
        {
            UserInfo UserInfo = new UserInfo();
            UserInfo.User = new JH_Auth_UserB().GetUserByPCCode(strSZHLCode);
            if (UserInfo.User != null)
            {
                UserInfo.UserRoleCode = new JH_Auth_UserRoleB().GetRoleCodeByUserName(UserInfo.User.UserName, UserInfo.User.ComId.Value);
                UserInfo.QYinfo = new JH_Auth_QYB().GetEntity(d => d.ComId == UserInfo.User.ComId.Value);
                UserInfo.BranchInfo = new JH_Auth_BranchB().GetBMByDeptCode(UserInfo.QYinfo.ComId, UserInfo.User.BranchCode);
                UserInfo.UserBMQXCode = GetQXBMByUserRole(UserInfo.QYinfo.ComId, UserInfo.UserRoleCode);
                if (UserInfo.UserBMQXCode == "")
                {
                    UserInfo.UserBMQXCode = (UserInfo.BranchInfo.DeptCode.ToString() + "," + UserInfo.BranchInfo.Remark1.Replace('-', ',')).TrimEnd(',');
                }


            }

            return UserInfo;
        }
        public UserInfo GetUserInfo(int intComid, string strUserName)
        {
            UserInfo UserInfo = new UserInfo();
            JH_Auth_User User = new JH_Auth_UserB().GetUserByUserName(intComid, strUserName);
            UserInfo.User = User;
            UserInfo.UserRoleCode = new JH_Auth_UserRoleB().GetRoleCodeByUserName(UserInfo.User.UserName, UserInfo.User.ComId.Value);
            UserInfo.QYinfo = new JH_Auth_QYB().GetEntity(d => d.ComId == UserInfo.User.ComId.Value);
            UserInfo.BranchInfo = new JH_Auth_BranchB().GetBMByDeptCode(UserInfo.QYinfo.ComId, UserInfo.User.BranchCode);
            UserInfo.UserBMQXCode = GetQXBMByUserRole(UserInfo.QYinfo.ComId, UserInfo.UserRoleCode);
            if (UserInfo.UserBMQXCode == "")
            {
                UserInfo.UserBMQXCode = (UserInfo.BranchInfo.DeptCode.ToString() + "," + UserInfo.BranchInfo.Remark1.Replace('-', ',')).TrimEnd(',');
            }


            return UserInfo;
        }

        public JH_Auth_User GetUserByUserName(int ComID, string UserName)
        {
            JH_Auth_User branchmodel = new JH_Auth_User();
            branchmodel = new JH_Auth_UserB().GetEntity(d => d.ComId == ComID && d.UserName == UserName);
            return branchmodel;
        }
        public JH_Auth_User GetUserByPCCode(string PCCode)
        {
            JH_Auth_User branchmodel = new JH_Auth_User();
            branchmodel = new JH_Auth_UserB().GetEntity(d => d.pccode == PCCode);
            return branchmodel;
        }
        /// <summary>
        /// 根据角色获取当前角色拥有的部门权限
        /// </summary>
        /// <param name="ComID"></param>
        /// <param name="UserRoleCode"></param>
        /// <returns></returns>
        public string GetQXBMByUserRole(int ComID, string UserRoleCode)
        {
            DataTable dt = new JH_Auth_RoleB().GetDTByCommand("SELECT RoleQX FROM JH_Auth_Role WHERE ROleCode in (" + UserRoleCode + ")");
            List<string> ListQXBM = new List<string>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string strBMCodes = dt.Rows[i]["RoleQX"].ToString();
                foreach (string bmcode in strBMCodes.Split(','))
                {
                    if (!ListQXBM.Contains(bmcode))
                    {
                        ListQXBM.Add(bmcode);
                    }
                }
            }

            return ListQXBM.ListTOString(',');
        }

        public string GetUserRealName(int intComid, string strUserName)
        {
            JH_Auth_User User = new JH_Auth_UserB().GetUserByUserName(intComid, strUserName);
            if (User == null)
            {
                return "";
            }
            else
            {
                return User.UserRealName;
            }
        }

        public void UpdateloginDate(int ComId, string strUser)
        {
            string strSql = string.Format("UPDATE JH_Auth_User SET logindate='{0}' WHERE ComId={1} and UserName = '{2}'", DateTime.Now.ToString("yyyy-MM-dd HH:ss"), ComId, strUser.ToFormatLike());
            new JH_Auth_UserB().ExsSql(strSql);
        }

        public void UpdatePassWord(int ComId, string strUser, string strNewPassWord)
        {
            string strSql = string.Format("UPDATE JH_Auth_User SET UserPass='{0}' WHERE ComId={1} and UserName in ('{2}')", strNewPassWord, ComId, strUser.ToFormatLike());
            new JH_Auth_UserB().ExsSql(strSql);
        }


        /// <summary>
        /// 根据部门获取用户列表
        /// </summary>
        /// <param name="branchCode">部门编号</param>
        /// <param name="strFilter">姓名，部门，手机号</param>
        /// <returns></returns>
        public DataTable GetUserListbyBranch(int branchCode, string strFilter, int ComId)
        {
            JH_Auth_Branch branch = new JH_Auth_BranchB().GetBMByDeptCode(ComId, branchCode);
            string strSQL = "select u.*,b.DeptName,b.DeptCode from  JH_Auth_User u  inner join JH_Auth_Branch b on u.branchCode=b.DeptCode where 1=1 ";
            strSQL += string.Format(" And  u.branchCode={0} or b.Remark1 like '{1}%'", branchCode, (branch.Remark1 == "" ? "" : branch.Remark1 + "-") + branch.DeptCode);

            if (strFilter != "")
            {
                strSQL += string.Format(" And (u.UserName like '%{0}%'  or u.UserRealName like '%{0}%'  or b.DeptName like '%{0}%' or u.mobphone like '%{0}%')", strFilter);
            }
            DataTable dt = new JH_Auth_UserB().GetDTByCommand(strSQL + " ORDER by b.DeptCode,u.UserOrder asc");
            return dt;
        }
        /// <summary>
        /// 根据部门获取可用用户列表
        /// </summary>
        /// <param name="branchCode">部门编号</param>
        /// <param name="strFilter">姓名，部门，手机号</param>
        /// <param name="comId">公司ID</param>
        /// <returns></returns>
        public DataTable GetUserListbyBranchUse(int branchCode, string strFilter, JH_Auth_UserB.UserInfo UserInfo)
        {
            JH_Auth_Branch branch = new JH_Auth_BranchB().GetBMByDeptCode(UserInfo.User.ComId.Value, branchCode);
            string strQXWhere = string.Format(" And ( u.branchCode={0} or b.Remark1 like '{1}%')", branchCode, (branch.Remark1 == "" ? "" : branch.Remark1 + "-") + branch.DeptCode);
            string branchqx = new JH_Auth_BranchB().GetBranchQX(UserInfo);
            if (branch.DeptRoot == -1 && !string.IsNullOrEmpty(branchqx))
            {
                strQXWhere = " And (";
                int i = 0;
                foreach (int dept in branchqx.SplitTOInt(','))
                {
                    JH_Auth_Branch branchQX = new JH_Auth_BranchB().GetBMByDeptCode(UserInfo.QYinfo.ComId, dept);
                    strQXWhere += string.Format((i == 0 ? "" : "And") + "  ( u.branchCode!={0} and b.Remark1 NOT like '{1}%')", dept, (branchQX.Remark1 == "" ? "" : branchQX.Remark1 + "-") + branchQX.DeptCode);
                    i++;
                }
                strQXWhere += ")";
            }
            string strSQL = "select u.*,b.DeptName,b.DeptCode from  JH_Auth_User u  inner join JH_Auth_Branch b on u.branchCode=b.DeptCode where u.IsUse='Y' and u.ComId=" + UserInfo.User.ComId;
            strSQL += string.Format(" {0} ", strQXWhere);

            if (strFilter != "")
            {
                strSQL += string.Format(" And (u.UserName like '%{0}%'  or u.UserRealName like '%{0}%'  or b.DeptName like '%{0}%' or u.mobphone like '%{0}%')", strFilter);
            }
            DataTable dt = new JH_Auth_UserB().GetDTByCommand(strSQL + " order by b.DeptShort,ISNULL(u.UserOrder, 1000000) asc");
            return dt;
        }


        /// <summary>
        /// 找到用户的直属上级,先找用户表leader,再找部门leader
        /// </summary>
        /// <param name="strUserName"></param>
        /// <returns></returns>
        public string GetUserLeader(int ComId, string strUserName)
        {
            string strLeader = "";
            UserInfo UserInfo = this.GetUserInfo(ComId, strUserName);
            if (!string.IsNullOrEmpty(UserInfo.User.UserLeader))
            {
                strLeader = UserInfo.User.UserLeader;
            }
            else
            {
                strLeader = UserInfo.BranchInfo.BranchLeader;
            }

            return strLeader;
        }
        /// <summary>
        /// 获取当前人员负责的部门的下属人员
        /// </summary>
        /// <param name="ComId">公司Id</param>
        /// <param name="rootCode">UserInfo.BranchInfo.DeptRoot + "-" + UserInfo.BranchInfo.DeptCode，当前人的部门的上级+部门Code</param>
        /// <param name="userName">当前负责人</param>
        /// <returns></returns>
        public List<JH_Auth_User> GetChildrenUser(int ComId, int rootCode, int DeptCode, string userName)
        {
            string strSql = string.Format(" SELECT * from JH_Auth_Branch  where ComId={0} and (Remark1 like '{1}%' or DeptCode={2}) ", ComId, (rootCode == -1 ? "" : rootCode + "-") + DeptCode, DeptCode);
            DataTable dt = new JH_Auth_BranchB().GetDTByCommand(strSql);
            string branchCode = "";
            foreach (DataRow row in dt.Rows)
            {
                branchCode += row["DeptCode"] + ",";
            }
            if (!string.IsNullOrEmpty(branchCode))
            {

                branchCode = branchCode.Substring(0, branchCode.Length - 1);
                int[] branchs = branchCode.SplitTOInt(',');
                List<JH_Auth_User> dtUser = new JH_Auth_UserB().GetEntities(d => d.ComId == ComId && branchs.Contains(d.BranchCode) && d.UserName != userName).ToList();
                return dtUser;
            }
            return new List<JH_Auth_User>();
        }
        /// <summary>
        /// 获取当前人员负责的下属人员
        /// </summary>
        /// <param name="ComId">公司Id</param>
        /// <param name="userName">当前用户名</param>
        /// <returns></returns>

        public List<JH_Auth_User> GetUserBranchUsers(int ComId, string userName)
        {
            //当前负责人的下属列表
            List<JH_Auth_User> userList = new List<JH_Auth_User>();
            //获取当前登录人负责的部门
            List<JH_Auth_Branch> branchList = new JH_Auth_BranchB().GetEntities(d => d.ComId == ComId && d.BranchLeader == userName).ToList();
            foreach (JH_Auth_Branch branch in branchList)
            {
                List<JH_Auth_User> branchUsers = new JH_Auth_UserB().GetChildrenUser(ComId, branch.DeptRoot, branch.DeptCode, userName);
                userList = userList.Concat(branchUsers).ToList();
            }
            //获取当前登录人是直属上级的下属用户
            List<JH_Auth_User> userCList = new JH_Auth_UserB().GetEntities(d => d.ComId == ComId && d.UserLeader == userName).ToList();
            userList = userList.Concat(userCList).ToList();
            return userList;
        }
    }

    public class JH_Auth_User_CenterB : BaseEFDao<JH_Auth_User_Center>
    {
        /// <summary>
        /// 添加消息并发送微信消息
        /// </summary>
        /// <param name="UserInfo">用户信息</param>
        /// <param name="type">类型</param>
        /// <param name="title">标题</param>
        /// <param name="content">发送内容</param>
        /// <param name="Id">实体Id</param>
        /// <param name="JSR">接收人</param>
        public void SendMsg(JH_Auth_UserB.UserInfo UserInfo, string ModelCode, string content, string Id, string JSR, string type = "A", int PIID = 0, string IsCS = "N")
        {

            JH_Auth_Model Model = new JH_Auth_ModelB().GetModeByCode(ModelCode);
            JH_Auth_User_Center userCenter = new JH_Auth_User_Center();
            userCenter.ComId = UserInfo.QYinfo.ComId;
            userCenter.CRUser = UserInfo.User.UserName;
            userCenter.CRDate = DateTime.Now;
            userCenter.MsgContent = content;
            userCenter.MsgType = Model == null ? "" : Model.ModelName;
            userCenter.UserFrom = UserInfo.User.UserName;
            userCenter.isRead = 0;
            userCenter.DataId = Id;
            userCenter.MsgModeID = ModelCode;
            userCenter.MsgLink = GetMsgLink(ModelCode, type, Id.ToString(), PIID, UserInfo.User.ComId.Value);
            userCenter.wxLink = GetWXMsgLink(ModelCode, type, Id.ToString(), UserInfo.QYinfo);
            userCenter.isCS = IsCS;
            userCenter.Remark = PIID.ToString();
            string sendUser = "";
            List<string> jsrs = JSR.Split(',').Distinct().ToList();//去重接收人
            foreach (string people in jsrs)
            {
                if (!string.IsNullOrEmpty(people))
                {
                    userCenter.UserTO = people;
                    new JH_Auth_User_CenterB().Insert(userCenter);
                    sendUser += people + ",";
                }

            }

        }
        public string GetMsgLink(string modelCode, string type, string Id, int PIID, int ComId)
        {
            string url = "";
            JH_Auth_Common commonUrl = new JH_Auth_CommonB().GetEntity(d => d.ModelCode == modelCode && d.MenuCode == type);
            string flag = "?";
            if (commonUrl.Url2.IndexOf("?") > -1)
            {
                flag = "&";
            }
            if (commonUrl != null && !string.IsNullOrEmpty(commonUrl.Url2))
            {
                string[] modelArray = new string[] { "ccxj", "lcsp", "ycgl", "hygl", "jfbx", "jygl" };
                if (modelArray.Contains(modelCode.ToLower()))
                {
                    if (type == "B")//流程查看页面
                    {
                        url = commonUrl.Url2 + flag + "FormCode=" + modelCode.ToUpper() + "&pageType=view&PIID=" + PIID + "&ID=" + Id;
                    }
                    else
                    {
                        if (PIID != 0)
                        {
                            Yan_WF_PI pimodel = new Yan_WF_PIB().GetEntity(d => d.ID == PIID && d.ComId == ComId);
                            Yan_WF_PD pdmodel = new Yan_WF_PDB().GetEntity(d => d.ID == pimodel.PDID && d.ComId == ComId);
                            url = commonUrl.Url2 + flag + "FormCode=" + modelCode.ToUpper() + "&ID=" + Id + "&PIID=" + pimodel.ID + "&LCTYPE=" + pdmodel.ProcessType + "&PDID=" + pdmodel.ID;
                        }
                    }
                }
                else if ((modelCode + type).ToLower() == "dbrwb")
                {
                    url = commonUrl.Url2 + flag + "groupcode=" + Id;
                }
                else
                {
                    if (commonUrl.Url2.IndexOf("APP_ADD_WF") > -1)
                    {
                        url = commonUrl.Url2 + flag + "FormCode=" + modelCode.ToUpper() + "&pageType=view&ID=" + Id;
                    }
                    else
                    {
                        url = commonUrl.Url2 + flag + "ID=" + Id;
                    }

                }
            }
            return url;
        }
        public string GetWXMsgLink(string modelCode, string type, string Id, JH_Auth_QY Qyinfo)
        {
            //提醒事项的消息没有链接
            if (modelCode != "TXSX")
            {
                string url = "/View_Mobile/UI/UI_COMMON.html?funcode=" + modelCode + "_" + type + "_" + Id + "&corpId=" + Qyinfo.corpId;
                return url;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// 阅读消息
        /// </summary>
        /// <param name="UserInfo"></param>
        /// <param name="DataID"></param>
        /// <param name="ModelCode"></param>
        public void ReadMsg(JH_Auth_UserB.UserInfo UserInfo, int DataID, string ModelCode)
        {
            Task<string> TaskCover = Task.Factory.StartNew<string>(() =>
            {
                string strSql = string.Format("UPDATE JH_Auth_User_Center SET isRead='1',ReadDate=GETDATE() WHERE DataId='{0}'AND ComId='{1}' AND UserTO='{2}' AND  MsgModeID='{3}'", DataID, UserInfo.User.ComId.Value, UserInfo.User.UserName, ModelCode);
                object obj = new JH_Auth_User_CenterB().ExsSclarSql(strSql);
                return "";
            });
        }
    }


    //部门表
    public class JH_Auth_BranchB : BaseEFDao<JH_Auth_Branch>
    {

        public void AddBranch(JH_Auth_UserB.UserInfo UserInfo, JH_Auth_Branch branch, Msg_Result msg)
        {

            if (branch.DeptCode == 0)//DeptCode==0为添加部门
            {
                //获取要添加的部门名称是否存在，存在提示用户，不存在添加
                JH_Auth_Branch branch1 = new JH_Auth_BranchB().GetEntity(d => d.DeptName == branch.DeptName && d.ComId == UserInfo.User.ComId);
                if (branch1 != null)
                {
                    msg.ErrorMsg = "部门已存在";
                    return;
                }
                //获取上下级的Path，用于上级查找下级所有部门或用户
                branch.Remark1 = new JH_Auth_BranchB().GetBranchNo(UserInfo.User.ComId.Value, branch.DeptRoot);
                branch.ComId = UserInfo.User.ComId;
                branch.CRUser = UserInfo.User.UserName;
                branch.CRDate = DateTime.Now;
                //添加部门，失败提示用户，成功赋值微信部门Code并更新
                if (!new JH_Auth_BranchB().Insert(branch))
                {
                    msg.ErrorMsg = "添加部门失败";
                    return;
                }

                if (UserInfo.QYinfo.IsUseWX == "Y")
                {

                    WXHelp bm = new WXHelp(UserInfo.QYinfo);
                    int branid = bm.WX_CreateBranch(branch);
                    branch.WXBMCode = branid;
                    new JH_Auth_BranchB().Update(branch);



                }
                msg.Result = branch;
            }
            else//DeptCode不等于0时为修改部门
            {
                if (branch.DeptRoot != -1)
                {
                    branch.Remark1 = new JH_Auth_BranchB().GetBranchNo(UserInfo.User.ComId.Value, branch.DeptRoot);
                }
                if (UserInfo.QYinfo.IsUseWX == "Y" && branch.DeptRoot != -1)
                {
                    WXHelp bm = new WXHelp(UserInfo.QYinfo);
                    bm.WX_UpdateBranch(branch);
                }
                if (!new JH_Auth_BranchB().Update(branch))
                {
                    msg.ErrorMsg = "修改部门失败";
                    return;
                }
            }

        }

        public JH_Auth_Branch GetBMByDeptCode(int ComID, int DeptCode)
        {
            JH_Auth_Branch branchmodel = new JH_Auth_Branch();

            branchmodel = new JH_Auth_BranchB().GetEntity(d => d.ComId == ComID && d.DeptCode == DeptCode);

            return branchmodel;
        }


        public override bool Update(JH_Auth_Branch entity)
        {

            if (base.Update(entity))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override bool Delete(JH_Auth_Branch entity)
        {

            return base.Delete(entity);
        }



        /// <summary>
        /// 根据部门代码删除部门及部门人员
        /// </summary>
        /// <param name="intBranchCode"></param>
        public void DelBranchByCode(int intBranchCode)
        {
            new JH_Auth_BranchB().Delete(d => d.DeptCode == intBranchCode);
            new JH_Auth_UserB().Delete(d => d.BranchCode == intBranchCode);
        }

        /// <summary>
        /// 获取机构数用在assginuser.ASPX中
        /// </summary>
        /// <param name="intRoleCode">角色代码</param>
        /// <param name="intBranchCode">机构代码</param>
        /// <returns></returns>
        public string GetBranchUserTree(string CheckNodes, int intBranchCode)
        {
            StringBuilder strTree = new StringBuilder();
            var q = new JH_Auth_BranchB().GetEntities(d => d.DeptRoot == intBranchCode);
            foreach (var item in q)
            {
                strTree.AppendFormat("{{id:'{0}',pId:'{1}',name:'{2}',{3}}},", item.DeptCode, item.DeptRoot, item.DeptName, item.DeptRoot == -1 || item.DeptRoot == 0 ? "open:true" : "open:false");
                strTree.Append(GetUserByBranch(CheckNodes, item.DeptCode));
                strTree.Append(GetBranchUserTree(CheckNodes, item.DeptCode));
            }
            return strTree.Length > 0 ? strTree.ToString() : "";
        }

        public string GetUserByBranch(string CheckNodes, int intBranchCode)
        {
            StringBuilder strTree = new StringBuilder();
            var q = new JH_Auth_UserB().GetEntities(d => d.BranchCode == intBranchCode);
            foreach (var item in q)
            {
                strTree.AppendFormat("{{id:'{0}',pId:'{1}',name:'{2}',isUser:'{3}',{4}}},", item.UserName, intBranchCode, item.UserRealName, "Y", CheckNodes.SplitTOList(',').Contains(item.UserName) ? "checked:true" : "checked:false");
            }
            return strTree.Length > 0 ? strTree.ToString() : "";
        }


        /// <summary>
        /// 获取组织机构树
        /// </summary>
        /// <param name="intDeptCode">机构代码</param>
        /// <returns></returns>
        public string GetBranchTree(int intDeptCode, int comId, string checkval, string branchQX = "", int index = 0)
        {
            StringBuilder strTree = new StringBuilder();
            var q = new JH_Auth_BranchB().GetEntities(d => d.DeptRoot == intDeptCode && d.ComId == comId);
            foreach (var item in q)
            {
                if (branchQX.SplitTOInt(',').Contains(item.DeptCode))
                {
                    strTree.AppendFormat("{{id:'{0}',pId:'{1}',attr:'{2}',name:'{3}',leader:'{4}',isuse:'{5}',order:'{6}',{7},checked:{8}}},", item.DeptCode, item.DeptRoot, "Branch", item.DeptName, item.BranchLeader, item.Remark2, item.DeptShort, index == 0 ? "open:true" : "open:false", Array.IndexOf(checkval.Split(','), item.DeptCode.ToString()) > -1 ? "true" : "false");
                    strTree.Append(GetBranchTree(item.DeptCode, comId, checkval, branchQX, index));
                }
                index++;

            }
            return strTree.Length > 0 ? strTree.ToString() : "";
        }
        /// <summary>
        /// 获取组织机构树
        /// </summary>
        /// <param name="intDeptCode">机构代码</param>
        /// <returns></returns>
        public string GetTopBranchTree(int intDeptCode, int comId, string checkval, string branchQX = "", int index = 0)
        {
            StringBuilder strTree = new StringBuilder();
            var q = new JH_Auth_BranchB().GetEntities(d => d.DeptRoot == intDeptCode && d.ComId == comId);

            foreach (var item in q)
            {
                if (branchQX == "" || index == 1 || !branchQX.SplitTOInt(',').Contains(item.DeptCode))
                {
                    strTree.AppendFormat("{{id:'{0}',pId:'{1}',attr:'{2}',name:'{3}',leader:'{4}',isuse:'{5}',order:'{6}',{7},checked:{8}{9}}},", item.DeptCode, item.DeptRoot, "Branch", item.DeptName, item.BranchLeader, item.Remark2, item.DeptShort, index == 0 ? "open:true" : "open:false", Array.IndexOf(checkval.Split(','), item.DeptCode.ToString()) > -1 ? "true" : "false", index == 0 ? ",nocheck:true" : "");
                    if (index < 3)
                    {
                        index++;
                        strTree.Append(GetTopBranchTree(item.DeptCode, comId, checkval, branchQX, index));

                    }
                }
            }

            return strTree.Length > 0 ? strTree.ToString() : "";
        }
        /// <summary>
        /// 获取组织机构树
        /// </summary>
        /// <param name="intDeptCode">机构代码</param>
        /// <returns></returns>
        public DataTable GetBranchList(int intDeptCode, int comId, string branchQX = "", int index = 0)
        {
            DataTable dtRoot = new DataTable();
            DataTable dt = new JH_Auth_BranchB().GetDTByCommand("SELECT * from JH_Auth_Branch  where DeptRoot=" + intDeptCode + " and ComId=" + comId + " order by DeptShort DESC");
            dt.Columns.Add("ChildBranch", Type.GetType("System.Object"));
            foreach (DataRow row in dt.Rows)
            {
                int deptCode = int.Parse(row["DeptCode"].ToString());
                index++;
                if (branchQX == "" || index == 1)
                {
                    row["ChildBranch"] = GetBranchList(deptCode, comId, branchQX, index);
                }
                else
                {
                    if (branchQX.SplitTOInt(',').Contains(deptCode))
                    {
                        row.Delete();
                    }
                    else
                    {
                        row["ChildBranch"] = GetBranchList(deptCode, comId, branchQX, index);
                    }
                }
            }
            dt.AcceptChanges();
            if (dtRoot.Rows.Count > 0)
            {
                dtRoot.Rows[0]["ChildBranch"] = dt;
                return dtRoot;
            }
            else
            {
                return dt;
            }
        }
        /// <summary>
        /// 获取组织机构树
        /// </summary>
        /// <param name="intDeptCode">机构代码</param>
        /// <returns></returns>
        public string GetBranchTree(int intDeptCode, string checkValue)
        {
            string[] checkIds = checkValue.Split(',');
            StringBuilder strTree = new StringBuilder();
            var q = new JH_Auth_BranchB().GetEntities(d => d.DeptRoot == intDeptCode);
            foreach (var item in q)
            {
                strTree.AppendFormat("{{id:'{0}',pId:'{1}',attr:'{2}',name:'{3}',leader:'{4}',isuse:'{5}',order:'{6}',checked:{7}}},", item.DeptCode, item.DeptRoot, "Branch", item.DeptName, item.Remark1, item.Remark2, item.DeptShort, Array.IndexOf(checkIds, item.DeptCode.ToString()) > -1 ? "true" : "false");
                strTree.Append(GetBranchTree(item.DeptCode, checkValue));
            }
            return strTree.Length > 0 ? strTree.ToString() : "";
        }
        /// <summary>
        /// 获取组织机构树
        /// </summary>
        /// <param name="intDeptCode">机构代码</param>
        /// <returns></returns>
        public string GetBranchTreeNew(int intDeptCode, int comId, int index = 0)
        {

            StringBuilder strTree = new StringBuilder();
            if (index == 0)
            {
                JH_Auth_Branch branch = new JH_Auth_BranchB().GetBMByDeptCode(comId, intDeptCode);
                strTree.AppendFormat("{{id:'{0}',pId:'{1}',attr:'{2}',name:'{3}',leader:'{4}',isuse:'{5}',order:'{6}',{7}}},", branch.DeptCode, branch.DeptRoot, "Branch", branch.DeptName, branch.Remark1, branch.Remark2, branch.DeptShort, index == 0 ? "open:true" : "open:false");
                index++;
            }
            var q = new JH_Auth_BranchB().GetEntities(d => d.DeptRoot == intDeptCode && d.ComId == comId);
            foreach (var item in q)
            {
                strTree.AppendFormat("{{id:'{0}',pId:'{1}',attr:'{2}',name:'{3}',leader:'{4}',isuse:'{5}',order:'{6}',{7}}},", item.DeptCode, item.DeptRoot, "Branch", item.DeptName, item.Remark1, item.Remark2, item.DeptShort, index == 0 ? "open:true" : "open:false");
                index++;
                strTree.Append(GetBranchTreeNew(item.DeptCode, comId, index));
            }
            return strTree.Length > 0 ? strTree.ToString() : "";
        }




        //获取JSON用户信息

        /// <summary>
        /// 获取部门级别编号
        /// </summary>
        /// <param name="DeptRoot"></param>
        /// <returns></returns>
        public string GetBranchNo(int ComID, int DeptRoot)
        {
            string BranchNo = "";
            var BranchUP = new JH_Auth_BranchB().GetBMByDeptCode(ComID, DeptRoot);
            //如果添加的上级部门存在，并且添加的同级部门中存在数据，获取同级部门的最后一个编号+1
            if (BranchUP != null)
            {
                BranchNo = (BranchUP.Remark1 == "" ? "" : BranchUP.Remark1 + "-") + BranchUP.DeptCode;
            }
            else
            {
                //如果上级部门不存在,直接添加顶级部门
                BranchNo = BranchUP.DeptCode.ToString();
            }
            return BranchNo;
        }

        public class BranchUser
        {
            public int BranchID { get; set; }
            public string BranchName { get; set; }
            public string BranchFzr { get; set; }

            public List<BranchUser> SubBranch { get; set; }
            public List<JH_Auth_User> SubUsers { get; set; }

        }
        /// <summary>
        /// 获取当前部门不能查看的部门Ids
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="ComId"></param>
        /// <returns></returns>
        public string GetBranchQX(JH_Auth_UserB.UserInfo userInfo)
        {
            string strSql = string.Format("SELECT DeptCode from JH_Auth_Branch where ComId={0}  And ','+TXLQX+',' NOT LIKE '%,{1},%' and TXLQX!='' and TXLQX is NOT NULL and IsHasQX='Y'", userInfo.User.ComId, userInfo.User.BranchCode);
            DataTable dt = new JH_Auth_BranchB().GetDTByCommand(strSql);
            string qxbranch = "";
            foreach (DataRow row in dt.Rows)
            {
                qxbranch += row["DeptCode"] + ",";
            }
            qxbranch = qxbranch.Length > 0 ? qxbranch.Substring(0, qxbranch.Length - 1) : "";
            return qxbranch;
        }
    }





    //角色表
    public class JH_Auth_RoleB : BaseEFDao<JH_Auth_Role>
    {
        /// <summary>
        /// 获取当前角色不能查看的角色Ids
        /// </summary>
        /// <param name="userName"></param> 
        /// <returns></returns>
        public string GetRoleQX(JH_Auth_UserB.UserInfo userInfo)
        {

            string strSql = string.Format("SELECT RoleCode from  JH_Auth_Role where ComId={0}  and IsHasQX='Y' And ','+RoleQX+',' NOT LIKE '%,{1},%'", userInfo.User.ComId, userInfo.User.BranchCode);
            DataTable dt = new JH_Auth_BranchB().GetDTByCommand(strSql);
            string qxrole = "";
            foreach (DataRow row in dt.Rows)
            {
                qxrole += row["RoleCode"] + ",";
            }
            qxrole = qxrole.Length > 0 ? qxrole.Substring(0, qxrole.Length - 1) : "";
            return qxrole;
        }

        /// <summary>
        /// 获取角色树
        /// </summary>
        /// <param name="intRoleCode">角色代码</param>
        /// <returns></returns>
        public string GetRoleTree(int intRoleCode)
        {
            StringBuilder strTree = new StringBuilder();
            var q = new JH_Auth_RoleB().GetEntities(d => d.PRoleCode == intRoleCode);
            foreach (var item in q)
            {
                strTree.AppendFormat("{{id:'{0}',pId:'{1}',icon:'../../Image/admin/users.png',issys:'{2}',isuse:'{3}',name:'{4}',nodedec:'{5}'}},", item.RoleCode, item.PRoleCode, item.isSysRole, item.IsUse, item.RoleName, item.RoleDec);
                strTree.Append(GetRoleTree(item.RoleCode));
            }
            return strTree.Length > 0 ? strTree.ToString() : "";
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="intRoleCode">角色代码</param>
        /// <returns></returns>
        public string delRole(int intRoleCode, int ComId)
        {
            new JH_Auth_RoleB().Delete(d => intRoleCode == d.RoleCode && d.isSysRole != "Y" && d.ComId == ComId);
            new JH_Auth_UserRoleB().Delete(d => intRoleCode == d.RoleCode && d.ComId == ComId);
            new JH_Auth_RoleFunB().Delete(d => d.RoleCode == intRoleCode && d.ComId == ComId);
            return "Success";
        }

        public DataTable GetModelFun(int ComId, string RoleCode, string strModeID)
        {
            DataTable dt = new JH_Auth_UserRoleB().GetDTByCommand("SELECT  DISTINCT JH_Auth_Function.ID, JH_Auth_Function.ModelID,JH_Auth_Function.PageName,JH_Auth_Function.ExtData,JH_Auth_Function.PageUrl,JH_Auth_Function.FunOrder,JH_Auth_Function.PageCode,JH_Auth_Function.isiframe FROM JH_Auth_RoleFun INNER JOIN JH_Auth_Function ON JH_Auth_RoleFun.FunCode=JH_Auth_Function.ID WHERE RoleCode IN (" + RoleCode + ")  AND ModelID='" + strModeID + "' AND  JH_Auth_RoleFun.ComId=" + ComId + " and (JH_Auth_Function.ComId=" + ComId + " or JH_Auth_Function.ComId=0)  order by JH_Auth_Function.FunOrder");
            return dt;
        }


    }



    //用户角色表
    public class JH_Auth_UserRoleB : BaseEFDao<JH_Auth_UserRole>
    {


        /// <summary>
        /// 获取用户的角色代码
        /// </summary>
        /// <param name="strUserName">用户名</param>
        /// <returns></returns>
        public string GetRoleCodeByUserName(string strUserName, int ComId)
        {
            string strRoleCode = "";
            var q = new JH_Auth_UserRoleB().GetEntities(d => d.UserName == strUserName && d.ComId == ComId);
            foreach (var item in q)
            {
                strRoleCode = strRoleCode + item.RoleCode.ToString() + ",";
            }
            return strRoleCode.TrimEnd(','); ;
        }


        /// <summary>
        /// 根据角色获取相应用户
        /// </summary>
        /// <param name="intRoleCode"></param>
        /// <returns></returns>
        public string GetUserByRoleCode(int intRoleCode)
        {
            return new JH_Auth_UserRoleB().GetEntities(d => d.RoleCode == intRoleCode).Select(d => d.UserName).ToList().ListTOString(',');
        }

        public DataTable GetUserDTByRoleCode(int intRoleCode, int ComId)
        {
            DataTable dt = new JH_Auth_UserRoleB().GetDTByCommand(" SELECT JH_Auth_User.* FROM dbo.JH_Auth_UserRole ur inner join dbo.JH_Auth_User on ur.username=JH_Auth_User.username where  JH_Auth_User.IsUse='Y' And JH_Auth_User.ComId=" + ComId + " And  ur.rolecode= " + intRoleCode);
            return dt;
        }

    }






    public class JH_Auth_ZiDianB : BaseEFDao<JH_Auth_ZiDian>
    {

        private List<JH_Auth_ZiDian> GetZDList()
        {
            List<JH_Auth_ZiDian> ListData = CacheHelp.Get("zidian") as List<JH_Auth_ZiDian>;
            if (ListData != null)
            {
                return ListData;
            }
            else
            {
                ListData = base.GetALLEntities().ToList();
                CacheHelp.Set("zidian", ListData);
                return ListData;
            }
        }

        public override IEnumerable<JH_Auth_ZiDian> GetEntities(Expression<Func<JH_Auth_ZiDian, bool>> exp)
        {

            List<JH_Auth_ZiDian> ListData = this.GetZDList();
            return ListData.Where(exp.Compile());

        }

        /// <summary>
        /// 重写字典修改方法,先清除缓存再删除
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override bool Update(JH_Auth_ZiDian entity)
        {
            CacheHelp.Remove("zidian");
            return base.Update(entity);
        }

        public override bool Delete(JH_Auth_ZiDian entity)
        {
            CacheHelp.Remove("zidian");
            return base.Delete(entity);
        }

        public override bool Insert(JH_Auth_ZiDian entity)
        {
            CacheHelp.Remove("zidian");
            return base.Insert(entity);
        }
    }





    public class JH_Auth_LogB : BaseEFDao<JH_Auth_Log>
    {

        public void InsertLog(string Action, string LogContent, string ReMark, string strUser, string strUserName, int ComID, string strIP)
        {
            Task<string> TaskCover = Task.Factory.StartNew<string>(() =>
            {
                this.Insert(new JH_Auth_Log()
                {
                    ComId = ComID.ToString(),
                    LogType = Action,
                    LogContent = LogContent,
                    Remark = ReMark,
                    IP = strIP,
                    Remark1 = strUserName,
                    CRUser = strUser,
                    CRDate = DateTime.Now
                });
                return "";
            });
        }

    }


    public class JH_Auth_VersionB : BaseEFDao<JH_Auth_Version>
    {
        public DataTable GetLastVer(string strUserCode)
        {
            DataTable dt = new JH_Auth_UserRoleB().GetDTByCommand("SELECT TOP 1 *  from JH_Auth_Version WHERE ','+ReadUsers+','NOT like '%," + strUserCode + ",%'   ORDER by id DESC");
            return dt;
        }

        public void SetUserVerSion(string strUserCode, string strVerID)
        {

            JH_Auth_Version Model = this.GetEntity(d => d.ID.ToString() == strVerID);
            Model.ReadUsers = Model.ReadUsers + "," + strUserCode;
            this.Update(Model);
        }
    }


    public class JH_Auth_QYB : BaseEFDao<JH_Auth_QY>
    {
        private List<JH_Auth_QY> GetQYList()
        {
            List<JH_Auth_QY> ListData = CacheHelp.Get("qydata") as List<JH_Auth_QY>;
            if (ListData != null)
            {
                return ListData;
            }
            else
            {
                ListData = base.GetALLEntities().ToList();
                CacheHelp.Set("qydata", ListData);
                return ListData;
            }
        }


        public override JH_Auth_QY GetEntity(Expression<Func<JH_Auth_QY, bool>> exp)
        {
            return this.GetQYList().Where(exp.Compile()).FirstOrDefault();
        }

        public override bool Update(JH_Auth_QY entity)
        {
            CacheHelp.Remove("qydata");
            return base.Update(entity);

        }
        public override bool Delete(JH_Auth_QY entity)
        {
            CacheHelp.Remove("qydata");
            return base.Delete(entity);
        }

    }
    public class JH_Auth_UserCustomDataB : BaseEFDao<JH_Auth_UserCustomData>
    {

    }
    public class JH_Auth_RoleFunB : BaseEFDao<JH_Auth_RoleFun>
    { }

    public class JH_Auth_FunctionB : BaseEFDao<JH_Auth_Function>
    { }
    #endregion

    #region 流程处理模块

    public class Yan_WF_DaiLiB : BaseEFDao<Yan_WF_DaiLi>
    { }

    public class Yan_WF_TDB : BaseEFDao<Yan_WF_TD>
    {

    }
    public class Yan_WF_PDB : BaseEFDao<Yan_WF_PD>
    {
        /// <summary>
        ///获取流程Id
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        public int GetProcessID(string processName)
        {
            return new Yan_WF_PDB().GetEntity(d => d.ProcessName == processName).ID;
        }
    }



    public class Yan_WF_PIB : BaseEFDao<Yan_WF_PI>
    {
        /// <summary>
        /// 添加流程()
        /// </summary>
        /// <param name="strAPPCode"></param>
        /// <param name="strSHR"></param>
        /// <returns>返回创建的第一个任务</returns>
        public Yan_WF_TI StartWF(Yan_WF_PD PD, string strModelCode, string userName, string strSHR, string strCSR, ref List<string> ListNextUser)
        {

            //创建流程实例
            Yan_WF_PI PI = new Yan_WF_PI();
            PI.WFFormNum = strModelCode;
            PI.PDID = PD.ID;
            PI.ComId = PD.ComId;
            PI.StartTime = DateTime.Now;
            PI.CRUser = userName;
            PI.CRDate = DateTime.Now;
            PI.PITYPE = PD.ProcessType;
            PI.ChaoSongUser = strCSR;
            new Yan_WF_PIB().Insert(PI);
            //创建流程实例
            Yan_WF_TI TI = new Yan_WF_TI();

            if (PD.ProcessType == "0")//自由流程
            {
                //添加首任务
                TI.TDCODE = "-1";
                TI.PIID = PI.ID;
                TI.ComId = PD.ComId;
                TI.StartTime = DateTime.Now;
                TI.EndTime = DateTime.Now;
                TI.TaskUserID = userName;
                TI.TaskUserView = "发起表单";
                TI.TaskState = 1;//任务已结束
                new Yan_WF_TIB().Insert(TI);
                //添加首任务

                TI.TaskUserID = strSHR;
                TI.EndTime = null;
                TI.TaskUserView = "";
                TI.TaskState = 0;//任务已结束
                new Yan_WF_TIB().Insert(TI);
                ListNextUser.Add(strSHR);
            }
            if (PD.ProcessType == "1")//固定流程
            {
                //添加首任务
                TI.TDCODE = PI.PDID.ToString() + "-1";
                TI.PIID = PI.ID;
                TI.StartTime = DateTime.Now;
                TI.EndTime = DateTime.Now;
                TI.TaskUserID = userName;
                TI.TaskUserView = "发起表单";
                TI.TaskState = 1;//任务已结束
                TI.ComId = PD.ComId;
                new Yan_WF_TIB().Insert(TI);
                //添加首任务
                ListNextUser = AddNextTask(TI);

            }
            return TI;

        }



        /// <summary>
        /// 结束当前任务
        /// </summary>
        /// <param name="TaskID"></param>
        /// <param name="strManAgeUser"></param>
        /// <param name="strManAgeYJ"></param>
        private void ENDTASK(int TaskID, string strManAgeUser, string strManAgeYJ, int Status = 1)
        {
            Yan_WF_TI TI = new Yan_WF_TIB().GetEntity(d => d.ID == TaskID);
            TI.TaskUserID = strManAgeUser;
            TI.TaskUserView = strManAgeYJ;
            TI.EndTime = DateTime.Now;
            TI.TaskState = Status;
            new Yan_WF_TIB().Update(TI);
            new Yan_WF_PIB().ExsSclarSql("UPDATE Yan_WF_TI SET TaskState=" + Status + " WHERE PIID='" + TI.PIID + "' AND TDCODE='" + TI.TDCODE + "'");//将所有任务置为结束状态
        }

        /// <summary>
        /// 结束当前流程
        /// </summary>
        /// <param name="PID"></param>
        public void ENDWF(int PID)
        {
            Yan_WF_PI PI = new Yan_WF_PIB().GetEntity(d => d.ID == PID);
            PI.isComplete = "Y";
            PI.CompleteTime = DateTime.Now;
            new Yan_WF_PIB().Update(PI);
        }


        /// <summary>
        /// 添加下一任务节点
        /// </summary>
        /// <param name="PID"></param>
        private List<string> AddNextTask(Yan_WF_TI TI, string strZSUser = "")
        {
            List<string> ListNextUser = new List<string>();

            if (strZSUser != "")
            {
                Yan_WF_TI Node = new Yan_WF_TI();
                Node.TDCODE = "-1";
                Node.PIID = TI.PIID;
                Node.StartTime = DateTime.Now;
                Node.TaskUserID = strZSUser;
                Node.TaskState = 0;//任务待结束
                Node.ComId = TI.ComId;
                new Yan_WF_TIB().Insert(Node);
                ListNextUser.Add(strZSUser);
            }
            else
            {
                string strNextTcode = TI.TDCODE.Split('-')[0] + "-" + (int.Parse(TI.TDCODE.Split('-')[1]) + 1).ToString();//获取任务CODE编码,+1即为下个任务编码
                Yan_WF_TD TD = new Yan_WF_TD();
                TD = new Yan_WF_TDB().GetEntity(d => d.TDCODE == strNextTcode);


                if (TD != null)
                {
                    if (TD.isSJ == "0")//选择角色时找寻角色人员
                    {
                        DataTable dt = new JH_Auth_UserRoleB().GetUserDTByRoleCode(Int32.Parse(TD.AssignedRole), TI.ComId.Value);
                        foreach (DataRow dr in dt.Rows)
                        {
                            Yan_WF_TI Node = new Yan_WF_TI();
                            Node.TDCODE = strNextTcode;
                            Node.PIID = TI.PIID;
                            Node.StartTime = DateTime.Now;
                            Node.TaskUserID = dr["username"].ToString();
                            Node.TaskState = 0;//任务待结束
                            Node.TaskName = TD.TaskName;
                            Node.TaskRole = TD.TaskAssInfo;
                            Node.ComId = TI.ComId;
                            Node.CRDate = DateTime.Now;
                            new Yan_WF_TIB().Insert(Node);
                            ListNextUser.Add(dr["username"].ToString());
                        }
                    }
                    Yan_WF_PI PI = new Yan_WF_PIB().GetEntity(d => d.ID == TI.PIID);
                    if (TD.isSJ == "1")//选择上级时找寻上级
                    {
                        string Leader = new JH_Auth_UserB().GetUserLeader(PI.ComId.Value, PI.CRUser);
                        Yan_WF_TI Node = new Yan_WF_TI();
                        Node.TDCODE = strNextTcode;
                        Node.PIID = TI.PIID;
                        Node.StartTime = DateTime.Now;
                        Node.TaskUserID = Leader;
                        Node.TaskState = 0;//任务待结束
                        Node.TaskName = TD.TaskName;
                        Node.TaskRole = TD.TaskAssInfo;
                        Node.ComId = TI.ComId;
                        Node.CRDate = DateTime.Now;
                        new Yan_WF_TIB().Insert(Node);
                        ListNextUser.Add(Leader);

                    }
                    if (TD.isSJ == "2")//选择发起人时找寻发起人
                    {


                        Yan_WF_TI Node = new Yan_WF_TI();
                        Node.TDCODE = strNextTcode;
                        Node.PIID = TI.PIID;
                        Node.StartTime = DateTime.Now;
                        Node.TaskUserID = PI.CRUser;
                        Node.TaskState = 0;//任务待结束
                        Node.TaskName = TD.TaskName;
                        Node.TaskRole = TD.TaskAssInfo;
                        Node.ComId = TI.ComId;
                        Node.CRDate = DateTime.Now;
                        new Yan_WF_TIB().Insert(Node);
                        ListNextUser.Add(PI.CRUser);
                    }
                    if (TD.isSJ == "3")//选择指定人员找指定人
                    {
                        foreach (string user in TD.AssignedRole.TrimEnd(',').Split(','))
                        {

                            Yan_WF_TI Node = new Yan_WF_TI();
                            Node.TDCODE = strNextTcode;
                            Node.PIID = TI.PIID;
                            Node.StartTime = DateTime.Now;
                            Node.TaskUserID = user;
                            Node.TaskState = 0;//任务待结束
                            Node.TaskName = TD.TaskName;
                            Node.TaskRole = TD.TaskAssInfo;
                            Node.ComId = TI.ComId;
                            Node.CRDate = DateTime.Now;
                            new Yan_WF_TIB().Insert(Node);
                            ListNextUser.Add(user);
                        }
                    }
                }
            }


            return ListNextUser;

        }
        /// <summary>
        /// 退回当前流程
        /// </summary>
        /// <param name="PID"></param>
        private void REBACKWF(int PID)
        {
            Yan_WF_PI PI = new Yan_WF_PIB().GetEntity(d => d.ID == PID);
            PI.IsCanceled = "Y";
            PI.CanceledTime = DateTime.Now;
            new Yan_WF_PIB().Update(PI);

        }

        /// <summary>
        /// 获取需要处理的任务
        /// </summary>
        /// <param name="strUser">用户需要处理</param>
        /// <returns></returns>
        public List<Yan_WF_TI> GetDSH(JH_Auth_User User)
        {
            List<Yan_WF_TI> ListData = new List<Yan_WF_TI>();

            ListData = new Yan_WF_TIB().GetEntities("ComId ='" + User.ComId.Value + "' AND TaskUserID ='" + User.UserName + "'AND TaskState='0'").ToList();
            return ListData;
        }



        public DataTable GetDSH_SY(JH_Auth_User User)
        {
            DataTable ListData = new DataTable();
            ListData = new Yan_WF_TIB().GetDTByCommand("SELECT TI.ID,TI.PIID,WPI.CRUser,WPI.CRDate,PD.ProcessName,PD.ID AS PDID,QYMODEL.ModelID,MODEL.ModelCode FROM Yan_WF_TI TI INNER JOIN  Yan_WF_PI WPI ON  TI.PIID=WPI.ID  INNER JOIN Yan_WF_PD PD ON WPI.PDID=PD.ID INNER JOIN JH_Auth_QY_Model QYMODEL ON PD.ID=QYMODEL.PDID INNER JOIN JH_Auth_Model MODEL ON QYMODEL.ModelID=MODEL.ID  AND TI.ComId='" + User.ComId + "'  AND TI.TaskUserID='" + User.UserName + "' AND TI.TaskState=0 order by WPI.CRDate DESC");
            return ListData;
        }



        /// <summary>
        /// 判断当前用户当前流程是否可以审批
        /// </summary>
        /// <param name="strUser"></param>
        /// <param name="PIID"></param>
        /// <returns></returns>
        public string isCanSP(string strUser, int PIID)
        {
            DataTable dt = new Yan_WF_TIB().GetDTByCommand("SELECT ID FROM  dbo.Yan_WF_TI  WHERE PIID='" + PIID + "' AND TaskState='0' AND TaskUserID='" + strUser + "' ");
            return dt.Rows.Count > 0 ? "Y" : "N";
        }


        /// <summary>
        /// 判断用户是否有编辑表单得权限
        /// </summary>
        /// <param name="strUser"></param>
        /// <param name="PIID"></param>
        /// <returns></returns>
        public string isCanEdit(string strUser, int PIID)
        {
            DataTable dt = new Yan_WF_TIB().GetDTByCommand("SELECT Yan_WF_TD.isCanEdit FROM  dbo.Yan_WF_TI LEFT JOIN   Yan_WF_TD on  Yan_WF_TI.TDCODE=Yan_WF_TD.TDCODE  WHERE PIID='" + PIID + "' AND TaskState='0' AND TaskUserID='" + strUser + "'and isCanEdit='True' ");
            return dt.Rows.Count > 0 ? "Y" : "N";

        }


        /// <summary>
        /// 退回流程
        /// </summary>
        /// <param name="strUser"></param>
        /// <param name="PIID"></param>
        /// <returns></returns>
        public bool REBACKLC(string strUser, int PIID, string strYJView)
        {
            try
            {
                Yan_WF_TI MODEL = new Yan_WF_TIB().GetEntities(" PIID='" + PIID + "' AND TaskState='0' AND TaskUserID='" + strUser + "' ").FirstOrDefault();
                if (MODEL != null)
                {
                    ENDTASK(MODEL.ID, strUser, strYJView, -1);//退回
                    REBACKWF(MODEL.PIID);
                }
                return true;
            }
            catch (Exception)
            {

                return false;
            }


        }

        /// <summary>
        /// 处理流程
        /// </summary>
        /// <param name="strUser"></param>
        /// <param name="PIID"></param>
        /// <returns></returns>
        public bool MANAGEWF(string strUser, int PIID, string strYJView, ref List<string> ListNextUser, string strShUser)
        {
            try
            {
                Yan_WF_TI MODEL = new Yan_WF_TIB().GetEntities(" PIID='" + PIID + "' AND TaskState='0' AND TaskUserID='" + strUser + "' ").FirstOrDefault();
                if (MODEL != null)
                {
                    ENDTASK(MODEL.ID, strUser, strYJView);
                    ListNextUser = AddNextTask(MODEL, strShUser);
                    //循环找下一个审核人是否包含本人，如果包含则审核通过,排除自由流程
                    if (ListNextUser.Contains(strUser) && MODEL.TDCODE != "-1")
                    {
                        ListNextUser.Clear();
                        return MANAGEWF(strUser, PIID, strYJView, ref ListNextUser, strShUser);
                    }
                }
                return true;
            }
            catch (Exception)
            {

                return false;
            }


        }

        /// <summary>
        /// 已处理的任务
        /// </summary>
        /// <param name="strUser"></param>
        /// <returns></returns>
        public List<Yan_WF_TI> GetYSH(JH_Auth_User User)
        {
            List<Yan_WF_TI> ListData = new List<Yan_WF_TI>();
            ListData = new Yan_WF_TIB().GetEntities(" ComId ='" + User.ComId.Value + "' AND TaskUserID ='" + User.UserName + "' AND (TaskState=1 OR TaskState=-1) AND TaskUserView!='发起表单'").ToList();
            return ListData;
        }
        /// <summary>
        /// 获取当前人创建并且已经审批的任务
        /// </summary>
        /// <param name="strUser"></param>
        /// <returns></returns>
        public List<Yan_WF_PI> GetYSHUserPI(string username, int comId, string formCode)
        {
            List<Yan_WF_PI> ListData = new List<Yan_WF_PI>();
            ListData = new Yan_WF_PIB().GetEntities("  CRUser='" + username + "' and isComplete='Y' and WFFormNum='" + formCode + "' and ComId=" + comId).ToList();
            return ListData;
        }
        public int GETPDID(int PIID)
        {
            Yan_WF_PI pi = new Yan_WF_PIB().GetEntity(d => d.ID == PIID);
            return pi == null ? 0 : pi.PDID.Value;
        }


        /// <summary>
        /// 更具PID获取PD数据
        /// </summary>
        /// <param name="PIID"></param>
        /// <returns></returns>
        public Yan_WF_PD GETPDMODELBYID(int PIID)
        {
            Yan_WF_PD MODEL = new Yan_WF_PD();
            Yan_WF_PI pi = new Yan_WF_PIB().GetEntity(d => d.ID == PIID);
            if (pi != null)
            {
                MODEL = new Yan_WF_PDB().GetEntity(d => d.ID == pi.PDID);
            }

            return MODEL;
        }

        /// <summary>
        /// 根据数据ID和modelCode更新PIID
        /// </summary>
        /// <param name="modelCode">modelCode</param>
        /// <param name="PIID">流程的PIID</param>
        /// <returns></returns>
        public string UpdateDataIdByCode(string modelCode, int DATAID, int PIID)
        {
            JH_Auth_Model model = new JH_Auth_ModelB().GetEntity(d => d.ModelCode == modelCode);
            if (model != null)
            {
                string strSql = string.Format(" UPDATE {0} SET intProcessStanceid={1} where ID={2}", model.RelTable, PIID, DATAID);
                object obj = new Yan_WF_PIB().ExsSclarSql(strSql);
                return obj != null ? obj.ToString() : "";
            }
            return "";
        }



        /// <summary>
        /// 根据PIID判断当前流程的数据（）
        /// </summary>
        /// <param name="PIID"></param>
        /// <returns></returns>
        public string GetPDStatus(int PIID)
        {
            Yan_WF_PI Model = new Yan_WF_PIB().GetEntity(d => d.ID == PIID);
            if (Model != null)
            {
                if (Model.isComplete == "Y")
                {
                    return "已审批";
                }
                if (Model.IsCanceled == "Y")
                {
                    return "已退回";
                }

                return "正在审批";
            }
            else
            {
                return "";
            }
        }
        public int GetFormIDbyPID(string strModeCode, int PID)
        {
            int intFormID = 0;

            JH_Auth_Model QYModel = new JH_Auth_ModelB().GetEntity(d => d.ModelCode == strModeCode);
            string strSQL = string.Format("SELECT ID FROM " + QYModel.RelTable + " WHERE  intProcessStanceid='{0}'", PID);
            intFormID = int.Parse(new Yan_WF_PDB().ExsSclarSql(strSQL) == null ? "0" : new Yan_WF_PDB().ExsSclarSql(strSQL).ToString());

            return intFormID;
        }


        /// <summary>
        /// 判断当前用户当前流程是否可以撤回到草稿箱,判断是不是只有一个处理过得节点,或者被退回的单据
        /// </summary>
        /// <param name="strUser"></param>
        /// <param name="PIID"></param>
        /// <returns>返回Y得时候可以撤回,返回不是Y,代表已经处理了不能撤回</returns>
        public string isCanCancel(string strUser, int PIID)
        {
            string strReturn = "N";
            if (GetPDStatus(PIID) == "已退回")
            {
                strReturn = "Y";
            }

            DataTable dt = new Yan_WF_TIB().GetDTByCommand("SELECT * FROM  dbo.Yan_WF_TI  WHERE PIID='" + PIID + "' AND EndTime IS not null");
            if (dt.Rows.Count == 1 && dt.Rows[0]["TaskUserID"].ToString() == strUser)
            {
                strReturn = "Y";
            }
            return strReturn;
        }





    }

    public class Yan_WF_TIB : BaseEFDao<Yan_WF_TI> { }
    public class SZHL_DBGLB : BaseEFDao<SZHL_DBGL> { }


    #endregion

    #region 企业号相关
    public class JH_Auth_WXPJB : BaseEFDao<JH_Auth_WXPJ>
    {
    }
    public class JH_Auth_WXMSGB : BaseEFDao<JH_Auth_WXMSG>
    {
    }

    public class JH_Auth_QY_ModelB : BaseEFDao<JH_Auth_QY_Model>
    {
        public JH_Auth_QY_Model GetQYModeByCode(int ComId, string strModeCode)
        {
            JH_Auth_QY_Model QYModel = new JH_Auth_QY_ModelB().GetEntity(d => d.QYModelCode == strModeCode);
            return QYModel;
        }

        public string isHasDataQX(string strModelCode, int DataID, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strISHasQX = "N";
            DataTable dt = new JH_Auth_ModelB().GetRelTableByModelCode(UserInfo.User.ComId.Value, strModelCode);
            if (dt.Rows.Count > 0)
            {
                string strPDID = dt.Rows[0]["PDID"].ToString();
                string strRelTable = dt.Rows[0]["RelTable"].ToString();
                bool noprocess = false;
                if (strPDID == "" || strPDID == "-1") //无流程时创建人才可编辑
                {
                    noprocess = true;
                }
                else
                {
                    if (DataID == 0)
                    {
                        string strSQL2 = string.Format("SELECT ProcessType FROM Yan_WF_PD WHERE ComId='{0}' AND ID='{1}'", UserInfo.User.ComId, strPDID);
                        object obj2 = new Yan_WF_PIB().ExsSclarSql(strSQL2);
                        string ProcessType = strSQL2 != null ? strSQL2.ToString() : "";
                        if (ProcessType == "-1")
                        {
                            noprocess = true;
                        }
                    }
                    else
                    {
                        noprocess = true;
                    }
                }
                if (noprocess)
                {
                    string strSQL = string.Format("SELECT CRUser FROM " + strRelTable + " WHERE ComID='{0}' AND   ID='{1}'", UserInfo.User.ComId, DataID);
                    object obj = new Yan_WF_PIB().ExsSclarSql(strSQL);
                    string CRUser = obj != null ? obj.ToString() : "";
                    if (UserInfo.User.UserName == CRUser)
                    {
                        strISHasQX = "Y";
                    }
                }

            }
            //else
            //{
            //    strISHasQX = "Y";
            //}
            return strISHasQX;
        }

        public string ISHASDATAREADQX(string strModelCode, int DataID, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strISHasQX = "Y";

            return strISHasQX;
        }

    }
    public class JH_Auth_ModelB : BaseEFDao<JH_Auth_Model>
    {
        public JH_Auth_Model GetModeByCode(string strModeCode)
        {
            JH_Auth_Model QYModel = new JH_Auth_ModelB().GetEntity(d => d.ModelCode == strModeCode);
            return QYModel;
        }

        /// <summary>
        /// 获取首页显示菜单
        /// </summary>
        /// <param name="UserInfo">用户信息</param>
        /// <param name="modelType">APPINDEX APP首页  PCINDEX PC首页</param>
        /// <returns></returns>
        public DataTable GETMenuList(JH_Auth_UserB.UserInfo UserInfo, string modelType = "APPINDEX")
        {
            if (!string.IsNullOrEmpty(UserInfo.UserRoleCode))
            {
                string strSql = string.Format(@"SELECT DISTINCT  model.*,qy.PDID,pd.ProcessType,qy.ID QYModelId,custom.ID UserAPPID,custom.DataContent1
                                             FROM JH_Auth_RoleFun rf INNER JOIN JH_Auth_Function fun on rf.FunCode=fun.ID and rf.ComId={0} and (fun.ComId={0} or fun.ComId=0)
                                            INNER join JH_Auth_Model model on fun.ModelID=model.ID AND (model.ComId={0} or model.ComId=0)
                                            inner join JH_Auth_QY_Model qy on model.ID=qy.ModelID and qy.ComId={0}
                                            LEFT join  JH_Auth_UserCustomData custom on model.ModelCode=custom.DataContent and custom.DataType='{1}' and custom.DataContent1='Y' and custom.UserName='{2}'and custom.ComId={0}
                                            LEFT JOIN  Yan_WF_PD pd on qy.PDID=pd.ID where qy.Status=1 and model.ModelStatus=0 and rf.RoleCode in ({3}) {4}", UserInfo.User.ComId, modelType, UserInfo.User.UserName, UserInfo.UserRoleCode, modelType == "APPINDEX" ? "and  WXUrl is not NULL  and  WXUrl!=''" : "");
                strSql = strSql + " ORDER by model.ORDERID ";
                return new JH_Auth_QY_ModelB().GetDTByCommand(strSql);
            }
            return new DataTable();
        }




        public DataTable GetRelTableByModelCode(int ComId, string modelCode)
        {
            string strSql = string.Format("SELECT RelTable,ISNULL(PDID,-1) AS  PDID  from  JH_Auth_QY_Model LEFT JOIN JH_Auth_Model ON JH_Auth_QY_Model.QYModelCode=JH_Auth_Model.ModelCode WHERE JH_Auth_QY_Model.ComId='{0}' AND JH_Auth_QY_Model.QYModelCode='{1}'", ComId, modelCode);
            DataTable dtResult = new Yan_WF_PIB().GetDTByCommand(strSql);
            return dtResult;
        }



    }
    public class JH_Auth_QY_WXSCB : BaseEFDao<JH_Auth_QY_WXSC>
    {
    }
    public class JH_Auth_YYLogB : BaseEFDao<JH_Auth_YYLog>
    {
    }
    #endregion


    #region 扩展字段
    public class JH_Auth_ExtendModeB : BaseEFDao<JH_Auth_ExtendMode>
    {
        //获取扩展字段的值
        public DataTable GetExtData(int? ComId, string FormCode, string DATAID, string PDID = "")
        {
            string strWhere = string.Empty;
            if (PDID != "") { strWhere = " and j.PDID='" + PDID + "'"; }
            return new JH_Auth_ExtendModeB().GetDTByCommand(string.Format("select j.ComId, j.ID, j.TableName, j.TableFiledColumn, j.TableFiledName, j.TableFileType, j.DefaultOption, j.DefaultValue, j.IsRequire, d.ExtendModeID, d.ID AS ExtID, d.DataID, d.ExtendDataValue from [dbo].[JH_Auth_ExtendMode] j join JH_Auth_ExtendData d on j.ComId=d.ComId and j.ID=d.ExtendModeID where j.ComId='{0}' and j.TableName='{1}' and d.DataID='{2}' and d.ExtendDataValue<>'' and d.ExtendDataValue is not null  " + strWhere, ComId, FormCode, DATAID));
        }

        public DataTable GetExtDataAll(int? ComId, string FormCode, string DATAID, string PDID = "")
        {
            string strWhere = string.Empty;
            if (PDID != "") { strWhere = " and j.PDID='" + PDID + "'"; }
            return new JH_Auth_ExtendModeB().GetDTByCommand(string.Format("select j.ComId, j.ID, j.TableName, j.TableFiledColumn, j.TableFiledName, j.TableFileType, j.DefaultOption, j.DefaultValue, j.IsRequire, d.ExtendModeID, d.ID AS ExtID, d.DataID, d.ExtendDataValue from [dbo].[JH_Auth_ExtendMode] j join JH_Auth_ExtendData d on j.ComId=d.ComId and j.ID=d.ExtendModeID where j.ComId='{0}' and j.TableName='{1}' and d.DataID='{2}' " + strWhere, ComId, FormCode, DATAID));
        }

        public DataTable GetExtColumnAll(int? ComId, string FormCode, string PDID = "")
        {
            string strWhere = string.Empty;
            if (PDID != "") { strWhere = " and j.PDID='" + PDID + "'"; }
            return new JH_Auth_ExtendModeB().GetDTByCommand(string.Format("select j.ComId, j.ID, j.TableName, j.TableFiledColumn, j.TableFiledName, j.TableFileType, j.DefaultOption, j.DefaultValue, j.IsRequire from [dbo].[JH_Auth_ExtendMode] j where j.ComId='{0}' and j.TableName='{1}'" + strWhere, ComId, FormCode));
        }
    }
    public class JH_Auth_ExtendDataB : BaseEFDao<JH_Auth_ExtendData>
    {
        public string GetExtIds(int ComId, string tablename, string content)
        {
            string ids = string.Empty;
            ArrayList al = new ArrayList();
            var list = new JH_Auth_ExtendDataB().GetEntities(p => p.ComId == ComId && p.TableName == tablename && p.ExtendDataValue.Contains(content));
            foreach (var l in list)
            {
                if (!al.Contains(l.DataID))
                {
                    al.Add(l.DataID);
                }
            }
            for (int i = 0; i < al.Count; i++)
            {
                if (string.IsNullOrEmpty(ids))
                {
                    ids = al[i].ToString();
                }
                else
                {
                    ids = ids + "," + al[i].ToString();
                }
            }
            return ids;
        }



        /// <summary>
        /// 获取导入excel的字段
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public List<IMPORTYZ> GetTable(string code, int comid)
        {
            string json = string.Empty;
            switch (code)
            {
                case "KHGL":

                    json = "[{\"Name\":\"客户名称\",\"Length\":\"50\",\"IsNull\":\"1\",\"IsRepeat\":\"SZHL_CRM_KHGL|KHName\"},{\"Name\":\"客户类型\",\"Length\":\"50\",\"IsNull\":\"0\"},"
                            + "{\"Name\":\"电话\",\"Length\":\"50\",\"IsNull\":\"0\"},{\"Name\":\"邮箱\",\"Length\":\"50\",\"IsNull\":\"0\"},"
                            + "{\"Name\":\"传真\",\"Length\":\"50\",\"IsNull\":\"0\"},{\"Name\":\"网址\",\"Length\":\"50\",\"IsNull\":\"0\"},"

                            + "{\"Name\":\"地址\",\"Length\":\"500\",\"IsNull\":\"0\"},"
                            + "{\"Name\":\"邮编\",\"Length\":\"50\",\"IsNull\":\"0\"},{\"Name\":\"跟进状态\",\"Length\":\"50\",\"IsNull\":\"0\"},"
                            + "{\"Name\":\"客户来源\",\"Length\":\"50\",\"IsNull\":\"0\"},{\"Name\":\"所属行业\",\"Length\":\"50\",\"IsNull\":\"0\"},"
                            + "{\"Name\":\"人员规模\",\"Length\":\"50\",\"IsNull\":\"0\"},{\"Name\":\"负责人\",\"Length\":\"50\",\"IsNull\":\"0\"},"
                            + "{\"Name\":\"备注\",\"Length\":\"0\",\"IsNull\":\"0\"}]";
                    break;
                case "KHLXR":

                    json = "[{\"Name\":\"姓名\",\"Length\":\"50\",\"IsNull\":\"1\"},{\"Name\":\"对应客户\",\"Length\":\"50\",\"IsNull\":\"0\",\"IsExist\":\"SZHL_CRM_KHGL|KHName\"},"
                        + "{\"Name\":\"手机\",\"Length\":\"11\",\"IsNull\":\"1\"},{\"Name\":\"邮箱\",\"Length\":\"50\",\"IsNull\":\"0\"},"
                        + "{\"Name\":\"传真\",\"Length\":\"50\",\"IsNull\":\"0\"},{\"Name\":\"网址\",\"Length\":\"50\",\"IsNull\":\"0\"},"
                        + "{\"Name\":\"电话\",\"Length\":\"50\",\"IsNull\":\"0\"},{\"Name\":\"分机\",\"Length\":\"50\",\"IsNull\":\"0\"},"
                        + "{\"Name\":\"QQ\",\"Length\":\"50\",\"IsNull\":\"0\"},{\"Name\":\"微信\",\"Length\":\"100\",\"IsNull\":\"0\"},"
                        //+ "{\"Name\":\"学历\",\"Length\":\"50\",\"IsNull\":\"0\"},{\"Name\":\"公司\",\"Length\":\"100\",\"IsNull\":\"0\"},"
                        + "{\"Name\":\"部门\",\"Length\":\"50\",\"IsNull\":\"0\"},{\"Name\":\"职位\",\"Length\":\"100\",\"IsNull\":\"0\"},"
                        + "{\"Name\":\"地址\",\"Length\":\"200\",\"IsNull\":\"0\"},"
                        + "{\"Name\":\"邮编\",\"Length\":\"50\",\"IsNull\":\"0\"},{\"Name\":\"性别\",\"Length\":\"10\",\"IsNull\":\"0\"},"
                        + "{\"Name\":\"生日\",\"Length\":\"10\",\"IsNull\":\"0\"},{\"Name\":\"备注\",\"Length\":\"0\",\"IsNull\":\"0\"}]";
                    break;
                case "HTGL":
                    json = "[{\"Name\":\"合同标题\",\"Length\":\"2500\",\"IsNull\":\"1\"},{\"Name\":\"合同类型\",\"Length\":\"50\",\"IsNull\":\"1\"},"
                        + "{\"Name\":\"合同总金额\",\"Length\":\"100\",\"IsNull\":\"1\"},{\"Name\":\"签约日期\",\"Length\":\"10\",\"IsNull\":\"0\"},"
                        + "{\"Name\":\"对应客户\",\"Length\":\"50\",\"IsNull\":\"0\"},{\"Name\":\"开始时间\",\"Length\":\"10\",\"IsNull\":\"0\"},"
                        + "{\"Name\":\"结束时间\",\"Length\":\"10\",\"IsNull\":\"0\"},{\"Name\":\"合同状态\",\"Length\":\"50\",\"IsNull\":\"0\"},"
                        + "{\"Name\":\"关联产品\",\"Length\":\"50\",\"IsNull\":\"0\"},{\"Name\":\"付款方式\",\"Length\":\"100\",\"IsNull\":\"0\"},"
                        + "{\"Name\":\"付款说明\",\"Length\":\"1500\",\"IsNull\":\"0\"},{\"Name\":\"有效期\",\"Length\":\"100\",\"IsNull\":\"0\"},"
                        + "{\"Name\":\"我方签约人\",\"Length\":\"50\",\"IsNull\":\"0\"},{\"Name\":\"客户方签约人\",\"Length\":\"50\",\"IsNull\":\"0\"},"
                        + "{\"Name\":\"合同编号\",\"Length\":\"100\",\"IsNull\":\"0\"},{\"Name\":\"负责人\",\"Length\":\"50\",\"IsNull\":\"0\"},"
                        + "{\"Name\":\"备注\",\"Length\":\"0\",\"IsNull\":\"0\"}]";
                    break;
            }

            if (comid != 0)
            {
                json = json.Substring(0, json.Length - 1);

                DataTable dtExtColumn = new JH_Auth_ExtendModeB().GetExtColumnAll(comid, code);
                foreach (DataRow drExt in dtExtColumn.Rows)
                {
                    json = json + ",{\"Name\":\"" + drExt["TableFiledName"].ToString() + "\",\"Length\":\"0\",\"IsNull\":\"0\"}";
                }

                json = json + "]";
            }

            List<IMPORTYZ> cls = JsonConvert.DeserializeObject<List<IMPORTYZ>>(json);
            return cls;

        }



        public class IMPORTYZ
        {
            public string Name { get; set; }
            public int Length { get; set; }
            public int IsNull { get; set; }
            public string IsRepeat { get; set; }
            public string IsExist { get; set; }
        }


        /// <summary>
        /// 获取模板中的默认数据
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string GetExcelData(string str)
        {
            string json = string.Empty;
            switch (str)
            {
                case "KHGL":
                    json = "贸易公司, 普通客户,010-65881997,123@qq.com, 010-65881998, http://www.baidu.com/,"
                        + "东三环中路101号,100000,初访,广告,服务,小于10人,13312345678,主营外贸销售，代理国外一线品牌";
                    break;
                case "KHLXR":
                    json = "张三,贸易公司,13667894321,123@qq.com,010-65881997 ,http://www.baidu.com/,010-65881997,61601 ,1123213213,fassfd21421,"
                        + "客户部,经理,东三环中路101号,100000,男,1983-09-13,负责XX项目的实施";
                    break;
                case "HTGL":
                    json = "XX项目,项目合同,12300,2015-07-08,贸易公司,2015-07-08,2015-12-08,未开始,企业号项目, 银行转账,"
                        + "服务,6个月,张经理,王经理,AC2001243251002,13312345678,合同备注";
                    break;
            }

            return json;
        }
    }

    #endregion

}

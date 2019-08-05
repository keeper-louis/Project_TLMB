using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.Permission.Objects;
using Kingdee.BOS.JSON;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.TLMB.Dynamic.DisplayPlugIn
{
    [Description("配送路线图展示")]
    public class DeliveryLine: AbstractDynamicFormPlugIn
    {
        public override void AfterCreateNewData(EventArgs e)
        {
            long personID = 0;
            base.AfterCreateNewData(e);
            string formId = "BD_Department";//部门FORMID
            Kingdee.BOS.Core.Metadata.FormMetadata formMetaData = MetaDataServiceHelper.GetFormMetaData(this.View.Context, formId);
            List<long> OrgIds = new List<long>();
            OrgIds.Add(this.Context.CurrentOrganizationInfo.ID);
            DataRuleFilterParamenter filterParameter = new DataRuleFilterParamenter(formId)
            {
                PermissionItemId = Kingdee.BOS.Core.Permission.PermissionConst.View,//查看权限项
                SubSystemId = formMetaData.BusinessInfo.GetForm().SubsysId,
                BusinessInfo = formMetaData.BusinessInfo,
                IsLookUp = true,//是否基础资料权限
                bzIsolateOrgIds = OrgIds,
                //OrgId = this.Context.CurrentOrganizationInfo.ID,
                ParentFormId = "PAEZ_DisPlayShow"//货架陈列展示FORMID
            };

            DataRuleFilterObject filterObject = PermissionServiceHelper.LoadDataRuleFilter(this.View.Context, filterParameter);//获取权限过滤条件
            Kingdee.BOS.Core.SqlBuilder.QueryBuilderParemeter queryParameter = new Kingdee.BOS.Core.SqlBuilder.QueryBuilderParemeter
            {
                FormId = formId,
                BusinessInfo = formMetaData.BusinessInfo,
                PermissionItemId = Kingdee.BOS.Core.Permission.PermissionConst.View,
                FilterClauseWihtKey = filterObject.FilterString,//过滤条件
            };

            Kingdee.BOS.Orm.DataEntity.DynamicObject[] stockObjects = BusinessDataServiceHelper.Load(this.View.Context, formMetaData.BusinessInfo.GetDynamicObjectType(), queryParameter);
            if (stockObjects.Count() > 0)
            {
                string strSql = string.Format(@"/*dialect*/SELECT FLINKOBJECT FROM T_SEC_USER WHERE FUSERID = {0}", this.View.Context.UserId);
                personID = DBUtils.ExecuteScalar<long>(this.View.Context, strSql, 0, null);
                if (personID > 0)
                {
                    string searchSql = string.Format(@"SELECT COUNT(1) FROM USER_TABLES WHERE TABLE_NAME = 'TABLE{0}'", personID);
                    int num = DBUtils.ExecuteScalar<int>(this.View.Context, searchSql, 0, null);
                    if (num > 0)
                    {
                        string dropSql = string.Format(@"/*dialect*/drop table TABLE{0}", personID);
                        DBUtils.Execute(this.View.Context, dropSql);
                    }
                    string createSql = string.Format(@"/*dialect*/create table TABLE{0}
(
  DEPTID NUMBER(10) not null,
  DEPTNAME   NVARCHAR2(255) not null
)", personID);
                    DBUtils.Execute(this.View.Context, createSql);
                    object[] stockId = (from c in stockObjects select c[0]).ToArray();
                    string ids = string.Join(",", stockId);
                    string insertSql = string.Format(@"/*dialect*/INSERT INTO TABLE{0} SELECT DEPT.FDEPTID,DEPTL.FNAME FROM T_BD_DEPARTMENT DEPT INNER JOIN T_BD_DEPARTMENT_L DEPTL ON DEPT.FDEPTID = DEPTL.FDEPTID WHERE DEPT.FDEPTID IN ({1})", personID, ids);
                    DBUtils.Execute(this.View.Context, insertSql);
                }

            }
            JSONObject webobj = new JSONObject();
            webobj["source"] = string.Format(@"http://221.180.255.112:9000/taoli/action2/delivery_deliveryLinebMapWeb.action?orgID={0}&orgName={1}&personID={2}", this.View.Context.CurrentOrganizationInfo.ID, this.View.Context.CurrentOrganizationInfo.Name, personID);
            webobj["height"] = 545;
            webobj["width"] = 810;
            webobj["isweb"] = true;  //是否新弹出一个浏览器窗口（or选项卡）打开网页地址
            webobj["title"] = "金蝶官网";
            this.View.AddAction("ShowKDWebbrowseForm", webobj);
            this.View.SendDynamicFormAction(this.View);
            this.View.Close();
        }
    }
}

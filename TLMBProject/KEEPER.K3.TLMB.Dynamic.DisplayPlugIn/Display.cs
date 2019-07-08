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
    [Description("货架陈列动态表单插件")]
    public class Display: AbstractDynamicFormPlugIn
    {
        public override void AfterCreateNewData(EventArgs e)
        {
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
                OrgId = this.Context.CurrentOrganizationInfo.ID,
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
            JSONObject webobj = new JSONObject();
            webobj["source"] = @"http://221.180.255.112:9000/taoli/action2/delivery_deliveryDisplayWeb.action";
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

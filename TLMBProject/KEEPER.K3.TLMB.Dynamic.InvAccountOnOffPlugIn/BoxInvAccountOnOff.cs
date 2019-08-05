using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Log;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.NetworkCtrl;
using Kingdee.BOS.Core.Permission;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.K3.Core.SCM.STK;
using Kingdee.K3.SCM.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.TLMB.Dynamic.InvAccountOnOffPlugIn
{
    [Description("箱套物料关账/反关账")]
    public class BoxInvAccountOnOff:AbstractDynamicFormPlugIn
    {
        // Fields
        private const string ActionEntityKey = "FEntityAction";//执行实体
        private bool bRecordMidData;
        private bool bShowErr;
        private const string ErrDetailEntityKey = "FEntityErrInfo";//其他错误信息实体
        private const string ErrTypeEntityKey = "FEntityErrType";//错误类型单据体
        private Dictionary<string, bool> ignoreCheckInfo = new Dictionary<string, bool>();
        private bool isbBusiness;
        private bool isClicked;
        private bool isOpenAccount;
        private const string MinusActionType = "Minus";
        private const string MinusEntityKey = "FMinusEntry";//负库存信息但具体
        private List<StockOrgOperateResult> opResults = new List<StockOrgOperateResult>();
        private const string StkBillDraftActionType = "StkBillDraft";
        private const string StkBillDraftEntryKey = "FStkDraftBillEntry";//暂存单据信息实体
        private const string StkCntBillAuditEntryKey = "FStkCountBillAuditEntry";//未审核盘点作业信息实体
        private const string StkCountBillAuditActionType = "CntBillAudit";

        // Methods
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.View.GetControl("FCLOSEDATE").Visible = !this.isOpenAccount;
            this.View.GetControl<Panel>("FPanelDate").Visible = !this.isOpenAccount;
            this.ShowHideErrTabDetail(null, ErrType.None);
        }

        public override void BarItemClick(BarItemClickEventArgs e)
        {
            string barItemKey = e.BarItemKey;
            if (barItemKey != null)
            {
                if (barItemKey != "tbAction")
                {
                    if (barItemKey != "tbErrDetail")
                    {
                        if (barItemKey == "tbExit")
                        {
                            this.View.Close();
                        }
                        return;
                    }
                }
                else
                {
                    this.DoAction();
                    this.isClicked = true;
                    return;
                }
                if (!this.bShowErr)
                {
                    this.ShowErrTypeInfo();
                }
                else
                {
                    this.ShowErrGrid(false);
                }
            }
        }

        private List<NetworkCtrlResult> BatchStartNetCtl(List<string> orgNum)
        {
            if (orgNum == null)
            {
                return null;
            }
            NetworkCtrlObject networkCtrlObj = null;
            List<NetworkCtrlResult> list = null;
            for (int i = 0; i < orgNum.Count; i++)
            {
                networkCtrlObj = NetworkCtrlServiceHelper.AddNetCtrlObj(base.Context, new LocaleValue(base.GetType().Name, 0x804), base.GetType().Name, base.GetType().Name + orgNum[i], NetworkCtrlType.BusinessObjOperateMutex, null, " ", true, true);
                NetworkCtrlServiceHelper.AddMutexNetCtrlObj(base.Context, networkCtrlObj.Id, networkCtrlObj.Id);
                NetWorkRunTimeParam para = new NetWorkRunTimeParam();
                NetworkCtrlResult item = NetworkCtrlServiceHelper.BeginNetCtrl(base.Context, networkCtrlObj, para);
                if (item.StartSuccess)
                {
                    if (list == null)
                    {
                        list = new List<NetworkCtrlResult>();
                    }
                    list.Add(item);
                }
                else
                {
                    this.View.ShowErrMessage(string.Format(ResManager.LoadKDString("网络冲突：库存组织编码[{0}]正处于关账或反关账，不允许操作！", "004023030002149", SubSystemType.SCM, new object[0]), orgNum[i]), "", MessageBoxType.Notice);
                    return list;
                }
            }
            return list;
        }
        
        private void ClearEntity(string entityKey)
        {
            Entity entryEntity = this.View.BusinessInfo.GetEntryEntity(entityKey);
            DynamicObjectType dynamicObjectType = entryEntity.DynamicObjectType;
            this.Model.GetEntityDataObject(entryEntity).Clear();
            this.View.UpdateView(entityKey);
        }

        public override void CreateNewData(BizDataEventArgs e)
        {
            DynamicObjectType dynamicObjectType = this.Model.BillBusinessInfo.GetDynamicObjectType();
            Entity entity = this.View.BusinessInfo.Entrys[1];
            DynamicObjectType dt = entity.DynamicObjectType;
            DynamicObject dataEntity = new DynamicObject(dynamicObjectType)
            {
                ["CloseDate"] = DateTime.Now.Date
            };
            DynamicObjectCollection objects = entity.DynamicProperty.GetValue<DynamicObjectCollection>(dataEntity);
            BusinessObject bizObject = new BusinessObject
            {
                Id = "STK_Account",
                PermissionControl = 1,
                SubSystemId = "STK"
            };
            List<long> valList = PermissionServiceHelper.GetPermissionOrg(base.Context, bizObject, this.isOpenAccount ? "4cc4dea42de6441ebeb21c509358d73d" : "1046d14017fd45dbaff9b1fe4affe0c6");
            if ((valList == null) || (valList.Count < 1))
            {
                e.BizDataObject = dataEntity;
            }
            else
            {
                Dictionary<string, object> batchStockDate = StockServiceHelper.GetBatchStockDate(base.Context, valList);
                if ((batchStockDate == null) || (batchStockDate.Keys.Count < 1))
                {
                    e.BizDataObject = dataEntity;
                }
                else
                {
                    valList.Clear();
                    foreach (string str in batchStockDate.Keys)
                    {
                        valList.Add(Convert.ToInt64(str));
                    }
                    List<SelectorItemInfo> list2 = new List<SelectorItemInfo> {
                    new SelectorItemInfo("FORGID"),
                    new SelectorItemInfo("FName"),
                    new SelectorItemInfo("FNumber"),
                    new SelectorItemInfo("FDescription")
                };
                    string str2 = this.GetInFilter(" FORGID", valList) + $" AND FDOCUMENTSTATUS = 'C' AND FFORBIDSTATUS = 'A' AND (FORGFUNCTIONS like'%103%') AND EXISTS(SELECT 1 FROM T_BAS_SYSTEMPROFILE BSP WHERE BSP.FCATEGORY = 'STK' AND BSP.FORGID = FORGID AND BSP.FACCOUNTBOOKID = 0 AND BSP.FKEY = 'IsInvEndInitial' AND BSP.FVALUE = '1') {(this.isOpenAccount ? "AND EXISTS(SELECT 1 FROM T_STK_CLOSEPROFILE SCP WHERE SCP.FCATEGORY = 'STK' AND SCP.FORGID = FORGID )" : "")} ";
                    QueryBuilderParemeter para = new QueryBuilderParemeter
                    {
                        FormId = "ORG_Organizations",
                        SelectItems = list2,
                        FilterClauseWihtKey = str2,
                        OrderByClauseWihtKey = "",
                        IsolationOrgList = null,
                        RequiresDataPermission = true
                    };
                    DynamicObjectCollection source = QueryServiceHelper.GetDynamicObjectCollection(base.Context, para, null);
                    DataTable stockOrgAcctLastCloseDate = CommonServiceHelper.GetStockOrgAcctLastCloseDate(base.Context, "");
                    Dictionary<long, DateTime> dictionary2 = new Dictionary<long, DateTime>();
                    foreach (DataRow row in stockOrgAcctLastCloseDate.Rows)
                    {
                        if (!(row["FCLOSEDATE"] is DBNull) && !string.IsNullOrWhiteSpace(row["FCLOSEDATE"].ToString()))
                        {
                            dictionary2[Convert.ToInt64(row["FORGID"])] = Convert.ToDateTime(row["FCLOSEDATE"]);
                        }
                    }
                    int num = 0;
                    if ((source != null) && (source.Count<DynamicObject>() > 0))
                    {
                        foreach (DynamicObject obj4 in source)
                        {
                            long key = Convert.ToInt64(obj4["FORGID"]);
                            DynamicObject item = new DynamicObject(dt)
                            {
                                ["Check"] = true,
                                ["StockOrgNo"] = obj4["FNumber"].ToString(),
                                ["StockOrgName"] = ((obj4["FName"] == null) || string.IsNullOrEmpty(obj4["FName"].ToString())) ? "" : obj4["FName"].ToString(),
                                ["StockOrgDesc"] = ((obj4["FDescription"] == null) || string.IsNullOrEmpty(obj4["FDescription"].ToString())) ? "" : obj4["FDescription"].ToString(),
                                ["StockOrgID"] = obj4["FORGID"].ToString(),
                                ["Result"] = "",
                                ["RetFlag"] = false,
                                ["Seq"] = num++
                            };
                            if (dictionary2.ContainsKey(key))
                            {
                                item["LastCloseDate"] = dictionary2[key];
                            }
                            objects.Add(item);
                        }
                    }
                    e.BizDataObject = dataEntity;
                }
            }
        }
        
        private void DoAction()
        {
            
            List<long> orgIds = new List<long>();
            List<string> orgNums = new List<string>();
            DynamicObjectCollection objects = this.View.Model.DataObject["EntityAction"] as DynamicObjectCollection;
            for (int i = 0; i < this.Model.GetEntryRowCount("FEntityAction"); i++)
            {
                if (Convert.ToBoolean(objects[i]["Check"]))
                {
                    orgIds.Add(Convert.ToInt64(objects[i]["StockOrgID"]));
                    orgNums.Add(objects[i]["StockOrgNo"].ToString());
                    this.Model.SetValue("FResult", "", i);
                }
            }
            List<StockOrgOperateResult> ret = this.DoOrgClose(orgIds, orgNums, false);
            this.MergeOperateResult(ret);
            this.RefreshOrgSuccessFlag();
            this.ShowErrTypeInfo();
        }

        private List<StockOrgOperateResult> DoOrgClose(List<long> orgIds, List<string> orgNums, bool isReDoAction)
        {
            if (orgIds.Count < 1)
            {
                this.View.ShowMessage(ResManager.LoadKDString("请先选择未成功处理过的库存组织", "004023030000247", SubSystemType.SCM, new object[0]), MessageBoxType.Notice);
                return null;
            }
            DateTime minValue = DateTime.MinValue;
            object obj2 = this.Model.GetValue("FCLOSEDATE");
            if (obj2 != null)
            {
                minValue = DateTime.Parse(obj2.ToString());
            }
            if ((minValue == DateTime.MinValue) && !this.isOpenAccount)
            {
                this.View.ShowMessage(ResManager.LoadKDString("请先录入关账日期", "004023030000244", SubSystemType.SCM, new object[0]), MessageBoxType.Notice);
                return null;
            }
            if (this.isbBusiness)
            {
                this.View.ShowMessage(ResManager.LoadKDString("上次提交未执行完毕，请稍后再试", "004023030002134", SubSystemType.SCM, new object[0]), MessageBoxType.Notice);
                return null;
            }
            this.isbBusiness = true;
            List<StockOrgOperateResult> list = null;
            List<NetworkCtrlResult> networkCtrlResults = this.BatchStartNetCtl(orgNums);
            if ((networkCtrlResults != null) && (networkCtrlResults.Count == orgNums.Count))
            {
                try
                {
                    bool flag = false;
                    bool flag2 = false;
                    bool flag3 = false;
                    if (isReDoAction)
                    {
                        this.ignoreCheckInfo.TryGetValue("Minus" + orgIds[0], out flag);
                        this.ignoreCheckInfo.TryGetValue("CntBillAudit" + orgIds[0], out flag2);
                        this.ignoreCheckInfo.TryGetValue("StkBillDraft" + orgIds[0], out flag3);
                    }
                    list = StockServiceHelper.InvAccountOnOff(base.Context, orgIds, minValue, this.isOpenAccount, !flag, !flag2, !flag3, this.bRecordMidData);
                }
                catch (Exception exception)
                {
                    this.View.ShowErrMessage(exception.Message, string.Format(ResManager.LoadKDString("执行{0}失败", "004023030002137", SubSystemType.SCM, new object[0]), this.isOpenAccount ? ResManager.LoadKDString("反关账", "004023030000238", SubSystemType.SCM, new object[0]) : ResManager.LoadKDString("关账", "004023030000241", SubSystemType.SCM, new object[0])), MessageBoxType.Notice);
                }
            }
            NetworkCtrlServiceHelper.BatchCommitNetCtrl(base.Context, networkCtrlResults);
            this.isbBusiness = false;
            return list;
        }

        private void DoSingleAction(string singleActionType)
        {
            List<long> orgIds = new List<long>();
            List<string> orgNums = new List<string>();
            DynamicObjectCollection objects = this.View.Model.DataObject["EntityAction"] as DynamicObjectCollection;
            int entryCurrentRowIndex = this.View.Model.GetEntryCurrentRowIndex("FEntityAction");
            long item = Convert.ToInt64(objects[entryCurrentRowIndex]["StockOrgID"]);
            orgIds.Add(item);
            orgNums.Add(objects[entryCurrentRowIndex]["StockOrgNo"].ToString());
            this.Model.SetValue("FResult", "", entryCurrentRowIndex);
            this.ignoreCheckInfo[singleActionType + item] = true;
            List<StockOrgOperateResult> ret = this.DoOrgClose(orgIds, orgNums, true);
            this.MergeOperateResult(ret);
            this.RefreshOrgSuccessFlag();
            this.ShowErrTypeInfo();
        }

        public override void EntityRowClick(EntityRowClickEventArgs e)
        {
            if (this.isClicked)
            {
                if (e.Key.Equals("FEntityAction", StringComparison.OrdinalIgnoreCase))
                {
                    this.ShowErrTypeInfo();
                }
                else if (e.Key.Equals("FEntityErrType", StringComparison.OrdinalIgnoreCase))
                {
                    this.ShowErrInfo();
                }
            }
        }

        public override void EntryBarItemClick(BarItemClickEventArgs e)
        {
            string barItemKey = e.BarItemKey;
            if (barItemKey != null)
            {
                if (barItemKey != "tbIgnoreMinus")
                {
                    if (barItemKey != "tbIgnoreStkBillAudit")
                    {
                        if (barItemKey == "tbIgnoreStkBillDraft")
                        {
                            this.DoSingleAction("StkBillDraft");
                        }
                        return;
                    }
                }
                else
                {
                    this.DoSingleAction("Minus");
                    return;
                }
                this.DoSingleAction("CntBillAudit");
            }
        }

        private string GetInFilter(string key, List<long> valList)
        {
            if ((valList != null) && (valList.Count >= 1))
            {
                return $" {key} in ({string.Join<long>(",", valList)})";
            }
            return $" {key} = -1 ";
        }

        private void MergeOperateResult(List<StockOrgOperateResult> ret)
        {
            if ((ret != null) && (ret.Count >= 1))
            {
                bool flag = false;
                foreach (StockOrgOperateResult result in ret)
                {
                    flag = false;
                    if (result.OperateSuccess)
                    {
                        if (this.ignoreCheckInfo.ContainsKey("Minus" + result.StockOrgID))
                        {
                            this.ignoreCheckInfo.Remove("Minus" + result.StockOrgID);
                        }
                        if (this.ignoreCheckInfo.ContainsKey("CntBillAudit" + result.StockOrgID))
                        {
                            this.ignoreCheckInfo.Remove("CntBillAudit" + result.StockOrgID);
                        }
                        if (this.ignoreCheckInfo.ContainsKey("StkBillDraft" + result.StockOrgID))
                        {
                            this.ignoreCheckInfo.Remove("StkBillDraft" + result.StockOrgID);
                        }
                    }
                    for (int i = 0; i < this.opResults.Count; i++)
                    {
                        if (this.opResults[i].StockOrgID == result.StockOrgID)
                        {
                            this.opResults[i] = result;
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        this.opResults.Add(result);
                    }
                }
            }
        }

        public override void OnInitialize(InitializeEventArgs e)
        {
            //第一个进入的方法
            base.OnInitialize(e);
            this.isOpenAccount = false;
            object customParameter = this.View.OpenParameter.GetCustomParameter("Direct");
            if (customParameter != null)
            {
                string str = customParameter.ToString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    string str2 = ResManager.LoadKDString("关账", "004023030000241", SubSystemType.SCM, new object[0]);//多语言设置，二开暂时不需要考虑
                    string str3 = ResManager.LoadKDString("反关账", "004023030000238", SubSystemType.SCM, new object[0]);//多语言设置，二开暂时不需要考虑
                    this.isOpenAccount = str.Equals("O", StringComparison.OrdinalIgnoreCase);//判断点击的菜单是关账还是反关账
                    this.View.SetFormTitle(new LocaleValue(this.isOpenAccount ? str3 : str2, base.Context.UserLocale.LCID));
                    this.View.SetInnerTitle(new LocaleValue(this.isOpenAccount ? str3 : str2, base.Context.UserLocale.LCID));
                    this.View.GetMainBarItem("tbAction").Text = this.isOpenAccount ? str3 : str2;//将元数据执行按钮设置它的显示内容，关账，反关账
                }
            }
            this.bShowErr = false;
            this.ShowErrGrid(this.bShowErr);
            this.View.GetControl<EntryGrid>("FEntityAction").SetFireDoubleClickEvent(true);//设置表格的双击事件，是否触发服务端
            //获取指定组织的指定系统参数，STK_StockParameter库存管理系统参数key，RecBalMidData库存关账记录中间结果key
            object obj3 = CommonServiceHelper.GetSystemProfile(base.Context, 0L, "STK_StockParameter", "RecBalMidData", false);
            this.bRecordMidData = false;
            if ((obj3 != null) && !string.IsNullOrWhiteSpace(obj3.ToString()))
            {
                this.bRecordMidData = Convert.ToBoolean(obj3);
            }
        }

        private void RefreshErrEntity(StockOrgOperateResult opResult, ErrType errType)
        {
            Entity entryEntity = this.View.BusinessInfo.GetEntryEntity("FEntityErrInfo");
            DynamicObjectType dynamicObjectType = entryEntity.DynamicObjectType;
            DynamicObjectCollection entityDataObject = this.Model.GetEntityDataObject(entryEntity);
            entityDataObject.Clear();
            if (((errType == ErrType.Minus) || (errType == ErrType.None)) || ((errType == ErrType.UnAuditStkCountBill) || (errType == ErrType.StkDraftBill)))
            {
                this.View.UpdateView("FEntityErrInfo");
            }
            else if (((opResult == null) || (opResult.ErrInfo == null)) || (opResult.ErrInfo.Count < 1))
            {
                this.View.UpdateView("FEntityErrInfo");
            }
            else
            {
                IEnumerable<OperateErrorInfo> enumerable;
                if (errType == ErrType.UnAuditBill)
                {
                    enumerable = from p in opResult.ErrInfo
                                 where p.ErrType == Convert.ToInt32(ErrType.UnAuditBill)
                                 select p;
                }
                else
                {
                    enumerable = from p in opResult.ErrInfo
                                 where p.ErrType < Convert.ToInt32(ErrType.OrgStatusErr)
                                 select p;
                }
                foreach (OperateErrorInfo info in enumerable)
                {
                    DynamicObject item = new DynamicObject(dynamicObjectType)
                    {
                        ["ErrType"] = info.ErrType,
                        ["ErrObjType"] = info.ErrObjType,
                        ["ErrObjKeyField"] = info.ErrObjKeyField,
                        ["ErrObjKeyID"] = info.ErrObjKeyID,
                        ["ErrMsg"] = info.ErrMsg
                    };
                    entityDataObject.Add(item);
                }
                this.View.UpdateView("FEntityErrInfo");
            }
        }

        private void RefreshMinusEntry(StockOrgOperateResult opResult, ErrType errType)
        {
            Entity entryEntity = this.View.BusinessInfo.GetEntryEntity("FMinusEntry");
            DynamicObjectType dynamicObjectType = entryEntity.DynamicObjectType;
            DynamicObjectCollection entityDataObject = this.Model.GetEntityDataObject(entryEntity);
            entityDataObject.Clear();
            if (((opResult != null) && (opResult.MinusErrObject != null)) && (((DynamicObjectCollection)opResult.MinusErrObject["Entry"]).Count > 0))
            {
                foreach (DynamicObject obj2 in (DynamicObjectCollection)opResult.MinusErrObject["Entry"])
                {
                    DynamicObject item = new DynamicObject(dynamicObjectType)
                    {
                        ["ErrMessage"] = obj2["ErrMessage"],
                        ["MaterialNumber"] = obj2["MaterialNumber"],
                        ["MaterialName"] = obj2["MaterialName"],
                        ["Specification"] = obj2["Specification"],
                        ["StockName"] = obj2["StockName"],
                        ["StockLocName"] = obj2["StockLocName"],
                        ["UnitName"] = obj2["UnitName"],
                        ["Qty"] = obj2["Qty"],
                        ["SecUnitName"] = obj2["SecUnitName"],
                        ["SecQty"] = obj2["SecQty"],
                        ["LotText"] = obj2["LotText"],
                        ["AuxPropName"] = obj2["AuxPropName"],
                        ["BOMNumber"] = obj2["BOMNumber"],
                        ["MtoNo"] = obj2["MtoNo"],
                        ["ProjectNo"] = obj2["ProjectNo"],
                        ["ProduceDate"] = obj2["ProduceDate"],
                        ["ExpiryDate"] = obj2["ExpiryDate"],
                        ["StockStatusName"] = obj2["StockStatusName"],
                        ["OwnerTypeName"] = obj2["OwnerTypeName"],
                        ["OwnerName"] = obj2["OwnerName"],
                        ["KeeperTypeName"] = obj2["KeeperTypeName"],
                        ["KeeperName"] = obj2["KeeperName"]
                    };
                    entityDataObject.Add(item);
                }
            }
            this.View.UpdateView("FMinusEntry");
        }

        private void RefreshOrgSuccessFlag()
        {
            DynamicObjectCollection entryDataObject = this.View.Model.DataObject["EntityAction"] as DynamicObjectCollection;
            DateTime minValue = DateTime.MinValue;
            object obj2 = this.Model.GetValue("FCLOSEDATE");
            if (obj2 != null)
            {
                minValue = DateTime.Parse(obj2.ToString());
            }
            DataTable stockOrgAcctLastCloseDate = null;
            if (this.isOpenAccount)
            {
                stockOrgAcctLastCloseDate = CommonServiceHelper.GetStockOrgAcctLastCloseDate(base.Context, "");
            }
            Func<StockOrgOperateResult, bool> predicate = null;
            for (int i = 0; i < entryDataObject.Count; i++)
            {
                if (Convert.ToBoolean(entryDataObject[i]["Check"]))
                {
                    if (predicate == null)
                    {
                        predicate = p => p.StockOrgID == Convert.ToInt64(entryDataObject[i]["StockOrgID"]);
                    }
                    StockOrgOperateResult result = this.opResults.SingleOrDefault<StockOrgOperateResult>(predicate);
                    if (result != null)
                    {
                        this.Model.SetValue("FResult", result.OperateSuccess ? ResManager.LoadKDString("成功", "004023030000250", SubSystemType.SCM, new object[0]) : ResManager.LoadKDString("失败", "004023030000253", SubSystemType.SCM, new object[0]), i);
                        this.Model.SetValue("FRetFlag", result.OperateSuccess, i);
                        if (result.OperateSuccess)
                        {
                            if (this.isOpenAccount)
                            {
                                DataRow[] source = stockOrgAcctLastCloseDate.Select($"FORGID={result.StockOrgID}");
                                if (source.Count<DataRow>() > 0)
                                {
                                    this.Model.SetValue("FLastCloseDate", source[0]["FCLOSEDATE"], i);
                                }
                                else
                                {
                                    this.Model.SetValue("FLastCloseDate", null, i);
                                    this.Model.SetValue("FCheck", false, i);
                                }
                            }
                            else
                            {
                                this.Model.SetValue("FLastCloseDate", minValue, i);
                            }
                        }
                        LogObject logObject = new LogObject
                        {
                            ObjectTypeId = this.View.BusinessInfo.GetForm().Id,
                            Description = string.Format(ResManager.LoadKDString("库存组织{0}{1}{2}{3}", "004023030000256", SubSystemType.SCM, new object[0]), new object[] {
                            result.StockOrgNumber,
                            result.StockOrgName,
                            this.isOpenAccount ? ResManager.LoadKDString("反关账", "004023030000238", SubSystemType.SCM, new object[0]) : ResManager.LoadKDString("关账", "004023030000241", SubSystemType.SCM, new object[0]),
                            result.OperateSuccess ? ResManager.LoadKDString("成功", "004023030000250", SubSystemType.SCM, new object[0]) : ResManager.LoadKDString("失败", "004023030000253", SubSystemType.SCM, new object[0])
                        }),
                            Environment = OperatingEnvironment.BizOperate,
                            OperateName = this.isOpenAccount ? ResManager.LoadKDString("反关账", "004023030000238", SubSystemType.SCM, new object[0]) : ResManager.LoadKDString("关账", "004023030000241", SubSystemType.SCM, new object[0]),
                            SubSystemId = "21"
                        };
                        this.Model.WriteLog(logObject);
                    }
                }
            }
        }

        private void RefreshStkBillDraftEntry(StockOrgOperateResult opResult, ErrType errtype)
        {
            Entity entryEntity = this.View.BusinessInfo.GetEntryEntity("FStkDraftBillEntry");
            DynamicObjectType dynamicObjectType = entryEntity.DynamicObjectType;
            DynamicObjectCollection entityDataObject = this.Model.GetEntityDataObject(entryEntity);
            entityDataObject.Clear();
            if (((opResult != null) && (opResult.StkBillDraftErrInfo != null)) && (opResult.StkBillDraftErrInfo.Count > 0))
            {
                foreach (OperateErrorInfo info in opResult.StkBillDraftErrInfo)
                {
                    DynamicObject item = new DynamicObject(dynamicObjectType)
                    {
                        ["DraftErrType"] = info.ErrType,
                        ["DraftErrObjType"] = info.ErrObjType,
                        ["DraftErrObjKeyField"] = info.ErrObjKeyField,
                        ["DraftErrObjKeyID"] = info.ErrObjKeyID,
                        ["DraftErrMsg"] = info.ErrMsg
                    };
                    entityDataObject.Add(item);
                }
            }
            this.View.UpdateView("FStkDraftBillEntry");
        }

        private void RefreshStkCntBillAuditEntry(StockOrgOperateResult opResult, ErrType errtype)
        {
            Entity entryEntity = this.View.BusinessInfo.GetEntryEntity("FStkCountBillAuditEntry");
            DynamicObjectType dynamicObjectType = entryEntity.DynamicObjectType;
            DynamicObjectCollection entityDataObject = this.Model.GetEntityDataObject(entryEntity);
            entityDataObject.Clear();
            if (((opResult != null) && (opResult.StkCountBillAuditErrInfo != null)) && (opResult.StkCountBillAuditErrInfo.Count > 0))
            {
                foreach (OperateErrorInfo info in opResult.StkCountBillAuditErrInfo)
                {
                    DynamicObject item = new DynamicObject(dynamicObjectType)
                    {
                        ["CtbaErrType"] = info.ErrType,
                        ["CtbaErrObjType"] = info.ErrObjType,
                        ["CtbaErrObjKeyField"] = info.ErrObjKeyField,
                        ["CtbaErrObjKeyID"] = info.ErrObjKeyID,
                        ["CtbaErrMsg"] = info.ErrMsg
                    };
                    entityDataObject.Add(item);
                }
            }
            this.View.UpdateView("FStkCountBillAuditEntry");
        }

        private void ShowErrGrid(bool isVisible)
        {
            this.View.GetControl<SplitContainer>("FSplitContainer").HideSecondPanel(!isVisible);//按照状态隐藏分割容器的第二个面板
            this.bShowErr = isVisible;
        }

        private void ShowErrInfo()
        {
            long curStockOrgId;
            if ((this.opResults == null) || (this.opResults.Count < 1))
            {
                this.ShowErrGrid(true);
            }
            else
            {
                int entryCurrentRowIndex = this.Model.GetEntryCurrentRowIndex("FEntityAction");
                curStockOrgId = Convert.ToInt64(this.Model.GetValue("FStockOrgID", entryCurrentRowIndex));
                StockOrgOperateResult opResult = this.opResults.SingleOrDefault<StockOrgOperateResult>(p => p.StockOrgID == curStockOrgId);
                entryCurrentRowIndex = this.Model.GetEntryCurrentRowIndex("FEntityErrType");
                ErrType none = ErrType.None;
                object obj2 = this.Model.GetValue("FErrorType", entryCurrentRowIndex);
                if ((obj2 != null) && !string.IsNullOrWhiteSpace(obj2.ToString()))
                {
                    Enum.TryParse<ErrType>(obj2.ToString(), out none);
                }
                this.RefreshErrEntity(opResult, none);
                this.RefreshStkBillDraftEntry(opResult, none);
                this.RefreshStkCntBillAuditEntry(opResult, none);
                this.RefreshMinusEntry(opResult, none);
                this.ShowHideErrTabDetail(opResult, none);
                this.ShowErrGrid(true);
            }
        }

        private void ShowErrTypeInfo()
        {
            long curStockOrgId;
            Entity entryEntity = this.View.BusinessInfo.GetEntryEntity("FEntityErrType");
            DynamicObjectType dynamicObjectType = entryEntity.DynamicObjectType;
            DynamicObjectCollection entityDataObject = this.Model.GetEntityDataObject(entryEntity);
            entityDataObject.Clear();
            if ((this.opResults == null) || (this.opResults.Count < 1))
            {
                this.ClearEntity("FEntityErrInfo");
                this.ShowHideErrTabDetail(null, ErrType.None);
                this.View.UpdateView("FEntityErrType");
                this.ShowErrGrid(true);
            }
            else
            {
                int entryCurrentRowIndex = this.Model.GetEntryCurrentRowIndex("FEntityAction");
                curStockOrgId = Convert.ToInt64(this.Model.GetValue("FStockOrgID", entryCurrentRowIndex));
                StockOrgOperateResult result = this.opResults.SingleOrDefault<StockOrgOperateResult>(p => p.StockOrgID == curStockOrgId);
                if ((result == null) || result.OperateSuccess)
                {
                    this.ClearEntity("FEntityErrInfo");
                    this.ShowHideErrTabDetail(null, ErrType.None);
                    this.View.UpdateView("FEntityErrType");
                    this.ShowErrGrid(true);
                }
                else
                {
                    DynamicObject obj2;
                    if ((result.ErrInfo != null) && result.ErrInfo.Exists(p => p.ErrType < Convert.ToInt32(ErrType.UnAuditBill)))
                    {
                        obj2 = new DynamicObject(dynamicObjectType)
                        {
                            ["ErrorType"] = ErrType.OrgStatusErr,
                            ["ErrTypeName"] = string.Format(ResManager.LoadKDString("当前组织状态不符合{0}操作条件", "004023030002140", SubSystemType.SCM, new object[0]), this.isOpenAccount ? ResManager.LoadKDString("反关账", "004023030000238", SubSystemType.SCM, new object[0]) : ResManager.LoadKDString("关账", "004023030000241", SubSystemType.SCM, new object[0]))
                        };
                        entityDataObject.Add(obj2);
                    }
                    if ((result.ErrInfo != null) && result.ErrInfo.Exists(p => p.ErrType == Convert.ToInt32(ErrType.UnAuditBill)))
                    {
                        obj2 = new DynamicObject(dynamicObjectType)
                        {
                            ["ErrorType"] = ErrType.UnAuditBill,
                            ["ErrTypeName"] = string.Format(ResManager.LoadKDString("当前组织存在未审核的库存单据", "004023030002143", SubSystemType.SCM, new object[0]), new object[0])
                        };
                        entityDataObject.Add(obj2);
                    }
                    if ((result.StkBillDraftErrInfo != null) && (result.StkBillDraftErrInfo.Count > 0))
                    {
                        obj2 = new DynamicObject(dynamicObjectType)
                        {
                            ["ErrorType"] = ErrType.StkDraftBill,
                            ["ErrTypeName"] = ResManager.LoadKDString("当前组织存在暂存的库存单据", "004023000022222", SubSystemType.SCM, new object[0])
                        };
                        entityDataObject.Add(obj2);
                    }
                    if ((result.StkCountBillAuditErrInfo != null) && (result.StkCountBillAuditErrInfo.Count > 0))
                    {
                        obj2 = new DynamicObject(dynamicObjectType)
                        {
                            ["ErrorType"] = ErrType.UnAuditStkCountBill,
                            ["ErrTypeName"] = ResManager.LoadKDString("当前组织存在未审核的盘点单据", "004023000018755", SubSystemType.SCM, new object[0])
                        };
                        entityDataObject.Add(obj2);
                    }
                    if (result.MinusErrObject != null)
                    {
                        obj2 = new DynamicObject(dynamicObjectType)
                        {
                            ["ErrorType"] = ErrType.Minus,
                            ["ErrTypeName"] = string.Format(ResManager.LoadKDString("当前组织存在异常库存数据", "004023030002146", SubSystemType.SCM, new object[0]), new object[0])
                        };
                        entityDataObject.Add(obj2);
                    }
                    this.View.UpdateView("FEntityErrType");
                    this.View.SetEntityFocusRow("FEntityErrType", 0);
                    this.ShowErrInfo();
                }
            }
        }

        private void ShowHideErrTabDetail(StockOrgOperateResult opResult, ErrType errType)
        {
            this.View.GetBarItem("FMinusEntry", "tbIgnoreMinus").Enabled = false;
            this.View.GetBarItem("FStkCountBillAuditEntry", "tbIgnoreStkBillAudit").Enabled = false;
            this.View.GetBarItem("FStkDraftBillEntry", "tbIgnoreStkBillDraft").Enabled = false;
            if (errType == ErrType.Minus)
            {
                this.View.GetControl<EntryGrid>("FStkCountBillAuditEntry").Visible = false;
                this.View.GetControl<EntryGrid>("FStkDraftBillEntry").Visible = false;
                this.View.GetControl<EntryGrid>("FEntityErrInfo").Visible = false;
                this.View.GetControl<EntryGrid>("FMinusEntry").Visible = true;
                if (((opResult != null) && (opResult.MinusErrObject != null)) && (Convert.ToInt32(opResult.MinusErrObject["ErrType"]) == 1))
                {
                    this.View.GetBarItem("FMinusEntry", "tbIgnoreMinus").Enabled = true;
                }
            }
            else if (errType == ErrType.UnAuditStkCountBill)
            {
                this.View.GetControl<EntryGrid>("FStkCountBillAuditEntry").Visible = true;
                this.View.GetControl<EntryGrid>("FStkDraftBillEntry").Visible = false;
                this.View.GetControl<EntryGrid>("FEntityErrInfo").Visible = false;
                this.View.GetControl<EntryGrid>("FMinusEntry").Visible = false;
                if (((opResult != null) && (opResult.StkCountBillAuditErrInfo != null)) && (opResult.StkCountBillAuditErrInfo.Count > 0))
                {
                    this.View.GetBarItem("FStkCountBillAuditEntry", "tbIgnoreStkBillAudit").Enabled = true;
                }
            }
            else if (errType == ErrType.StkDraftBill)
            {
                this.View.GetControl<EntryGrid>("FStkCountBillAuditEntry").Visible = false;
                this.View.GetControl<EntryGrid>("FStkDraftBillEntry").Visible = true;
                this.View.GetControl<EntryGrid>("FEntityErrInfo").Visible = false;
                this.View.GetControl<EntryGrid>("FMinusEntry").Visible = false;
                if (((opResult != null) && (opResult.StkBillDraftErrInfo != null)) && (opResult.StkBillDraftErrInfo.Count > 0))
                {
                    this.View.GetBarItem("FStkDraftBillEntry", "tbIgnoreStkBillDraft").Enabled = true;
                }
            }
            else
            {
                this.View.GetControl<EntryGrid>("FStkCountBillAuditEntry").Visible = false;
                this.View.GetControl<EntryGrid>("FStkDraftBillEntry").Visible = false;
                this.View.GetControl<EntryGrid>("FEntityErrInfo").Visible = true;
                this.View.GetControl<EntryGrid>("FMinusEntry").Visible = false;
            }
            this.View.UpdateView("FTabErrDetail");
        }

        // Properties
        public Dictionary<string, bool> IgnoreCheckInfo =>
            this.ignoreCheckInfo;

        public bool IsOpenAccount =>
            this.isOpenAccount;

        public List<StockOrgOperateResult> OperateResult =>
            this.opResults;

        // Nested Types
        protected enum ErrType
        {
            CloseDateErr = 3,
            FinCloseDate = 5,
            Minus = 0x3e8,
            NoInit = 4,
            None = -1,
            NoOrg = 1,
            OrgStatusErr = 0x63,
            StartDateErr = 2,
            StkDraftBill = 200,
            UnAuditBill = 100,
            UnAuditStkCountBill = 500
        }
    }
}

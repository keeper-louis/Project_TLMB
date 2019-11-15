using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.App;

namespace ZQC.K3.TLMB.DirectTransform.BusinessPlugIn
{
    [Description("直接调拨单表单插件")]
    //根据单据头为分销站，线索为空下，刷新已有的分录数据，调出仓库默认分销站仓库，调入仓库默认工厂成品库。限制箱套物料品，新增分录自动填充
    public class DirectTransformBill : AbstractBillPlugIn
    {


        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key = e.Field.Key.ToUpperInvariant();

            #region 对数量监听，实现仓库携带。
            if (key == "FQTY" && e.NewValue != null)
            {
                string transType = this.Model.GetValue("FAllocateType") as string;
                DynamicObject org = this.Model.GetValue("FStockOrgId") as DynamicObject;
                string orgId = Convert.ToString(org["Id"]);
                DynamicObject line = this.Model.GetValue("FLINE") as DynamicObject;
                DynamicObject salDept = this.Model.GetValue("FSALEDEPTID") as DynamicObject;

                string sql0 = string.Format(@"select FdeptId from T_BD_DEPARTMENT_L where fname like '%分销站%' ");
                DynamicObjectCollection result1 = DBUtils.ExecuteDynamicObject(this.Context, sql0);

                DynamicObject destStock = this.Model.GetValue("FDESTSTOCKID") as DynamicObject;

                if (result1 != null)
                {    //调拨类型
                    if (transType != null && transType == "0")
                    {
                        //线路为空
                        if (line == null)
                        {

                            if (salDept != null)
                            {
                                string deptId = Convert.ToString(salDept["Id"]);
                                foreach (DynamicObject item in result1)
                                {
                                    if (Convert.ToString(item["FdeptId"]) == deptId)
                                    {
                                        string sql1 = string.Format(@"select fstockid, fnumber
                                  from t_bd_stock
                                    where fdept = {0}
                                     and fstockid not in
                                       (select flinestock
                                          from t_tl_line
                                         where fdept = {0}
                                        union
                                        select freturnstock from t_tl_line where fdept = {0})", deptId);
                                        long result = DBUtils.ExecuteScalar<long>(base.Context, sql1, -1, null);
                                        IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
                                        IViewService view = ServiceHelper.GetService<IViewService>();//界面服务
                                        FormMetadata Meta = metaService.Load(base.Context, "BD_STOCK") as FormMetadata;//获取基础资料元数据
                                        DynamicObject BasicObject = view.LoadSingle(base.Context, result, Meta.BusinessInfo.GetDynamicObjectType());//不参与核算的库存状态ID

                                        string sql2 = string.Format(@"
                                           select t1.fstockId from t_bd_stock t1 
                                            inner join t_bd_stock_l t2 on t1.fstockid = t2.fstockid
                                            where t2.fname='工厂成品库' and t1.Fuseorgid={0}
                                        ", orgId);
                                        long result2 = DBUtils.ExecuteScalar<long>(base.Context, sql2, -1, null);
                                        DynamicObject BasicObject1 = view.LoadSingle(base.Context, result2, Meta.BusinessInfo.GetDynamicObjectType());

                                        //箱套类型
                                        int row = this.Model.GetEntryRowCount("FBillEntry");
                                        for (int i = 0; i < row; i++)
                                        {
                                            DynamicObject material = this.Model.GetValue("FMaterialId", i) as DynamicObject;
                                            if (material != null)
                                            {
                                                if (material["Number"].Equals("07040000") || material["Number"].Equals("07040004"))
                                                {
                                                    this.Model.SetValue("FDestStockId", BasicObject1, i);
                                                    this.Model.SetValue("FSRCSTOCKID", BasicObject, i);
                                                }
                                            }

                                        }



                                    }
                                }



                            }
                        }
                    }

                }



            }
            #endregion


            #region 对调拨类型监听，删除仓库
            if ((key == "FALLOCATETYPE" && e.NewValue != null) || (key == "FSALEDEPTID" && e.NewValue != null) )
            {
                DynamicObject org = this.Model.GetValue("FStockOrgId") as DynamicObject;
                string orgId = Convert.ToString(org["Id"]);
                DynamicObject line = this.Model.GetValue("FLINE") as DynamicObject;
                DynamicObject salDept = this.Model.GetValue("FSALEDEPTID") as DynamicObject;

                string sql0 = string.Format(@"select FdeptId from T_BD_DEPARTMENT_L where fname like '%分销站%' ");
                DynamicObjectCollection result1 = DBUtils.ExecuteDynamicObject(this.Context, sql0);
                string transType = this.Model.GetValue("FAllocateType") as string;
                //if (transType != null && transType != "0")
                //{
                //    int row = this.Model.GetEntryRowCount("FBillEntry");
                //    for (int i = 0; i < row; i++)
                //    {
                //        //DynamicObject stockId = this.Model.GetValue("FSTOCKID") as DynamicObject;
                //        this.Model.SetValue("FDestStockId", null, i);
                //        this.Model.SetValue("FSRCSTOCKID", null, i);
                //    }

                //}

                if (transType != null && transType == "0")
                {
                    //线路为空
                    if (line == null)
                    {

                        if (salDept != null)
                        {
                            string deptId = Convert.ToString(salDept["Id"]);
                            foreach (DynamicObject item in result1)
                            {
                                if (Convert.ToString(item["FdeptId"]) == deptId)
                                {
                                    string sql1 = string.Format(@"select fstockid, fnumber
                                  from t_bd_stock
                                    where fdept = {0}
                                     and fstockid not in
                                       (select flinestock
                                          from t_tl_line
                                         where fdept = {0}
                                        union
                                        select freturnstock from t_tl_line where fdept = {0})", deptId);
                                    long result = DBUtils.ExecuteScalar<long>(base.Context, sql1, -1, null);
                                    IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
                                    IViewService view = ServiceHelper.GetService<IViewService>();//界面服务
                                    FormMetadata Meta = metaService.Load(base.Context, "BD_STOCK") as FormMetadata;//获取基础资料元数据
                                    DynamicObject BasicObject = view.LoadSingle(base.Context, result, Meta.BusinessInfo.GetDynamicObjectType());//不参与核算的库存状态ID

                                    string sql2 = string.Format(@"
                                           select t1.fstockId from t_bd_stock t1 
                                            inner join t_bd_stock_l t2 on t1.fstockid = t2.fstockid
                                            where t2.fname='工厂成品库' and t1.Fuseorgid={0}
                                        ", orgId);
                                    long result2 = DBUtils.ExecuteScalar<long>(base.Context, sql2, -1, null);
                                    DynamicObject BasicObject1 = view.LoadSingle(base.Context, result2, Meta.BusinessInfo.GetDynamicObjectType());

                                    //箱套类型
                                    int row = this.Model.GetEntryRowCount("FBillEntry");
                                    for (int i = 0; i < row; i++)
                                    {
                                        DynamicObject material = this.Model.GetValue("FMaterialId", i) as DynamicObject;
                                        if (material != null)
                                        {
                                            if (material["Number"].Equals("07040000") || material["Number"].Equals("07040004"))
                                            {
                                                this.Model.SetValue("FDestStockId", BasicObject1, i);
                                                this.Model.SetValue("FSRCSTOCKID", BasicObject, i);
                                            }
                                        }

                                    }



                                }
                            }



                        }
                    }

                }

            }
            #endregion

            #region 对线路的监听
            if (key == "FLINE" && e.NewValue == null)
            {
                DynamicObject org = this.Model.GetValue("FStockOrgId") as DynamicObject;
                string orgId = Convert.ToString(org["Id"]);
                DynamicObject line = this.Model.GetValue("FLINE") as DynamicObject;
                DynamicObject salDept = this.Model.GetValue("FSALEDEPTID") as DynamicObject;

                string sql0 = string.Format(@"select FdeptId from T_BD_DEPARTMENT_L where fname like '%分销站%' ");
                DynamicObjectCollection result1 = DBUtils.ExecuteDynamicObject(this.Context, sql0);
                string transType = this.Model.GetValue("FAllocateType") as string;
                if (transType != null && transType == "0")
                {

                    if (salDept != null)
                    {
                        string deptId = Convert.ToString(salDept["Id"]);
                        foreach (DynamicObject item in result1)
                        {
                            if (Convert.ToString(item["FdeptId"]) == deptId)
                            {
                                string sql1 = string.Format(@"select fstockid, fnumber
                                  from t_bd_stock
                                    where fdept = {0}
                                     and fstockid not in
                                       (select flinestock
                                          from t_tl_line
                                         where fdept = {0}
                                        union
                                        select freturnstock from t_tl_line where fdept = {0})", deptId);
                                long result = DBUtils.ExecuteScalar<long>(base.Context, sql1, -1, null);
                                IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
                                IViewService view = ServiceHelper.GetService<IViewService>();//界面服务
                                FormMetadata Meta = metaService.Load(base.Context, "BD_STOCK") as FormMetadata;//获取基础资料元数据
                                DynamicObject BasicObject = view.LoadSingle(base.Context, result, Meta.BusinessInfo.GetDynamicObjectType());//不参与核算的库存状态ID

                                string sql2 = string.Format(@"
                                           select t1.fstockId from t_bd_stock t1 
                                            inner join t_bd_stock_l t2 on t1.fstockid = t2.fstockid
                                            where t2.fname='工厂成品库' and t1.Fuseorgid={0}
                                        ", orgId);
                                long result2 = DBUtils.ExecuteScalar<long>(base.Context, sql2, -1, null);
                                DynamicObject BasicObject1 = view.LoadSingle(base.Context, result2, Meta.BusinessInfo.GetDynamicObjectType());

                                //箱套类型
                                int row = this.Model.GetEntryRowCount("FBillEntry");
                                for (int i = 0; i < row; i++)
                                {
                                    DynamicObject material = this.Model.GetValue("FMaterialId", i) as DynamicObject;
                                    if (material != null)
                                    {
                                        if (material["Number"].Equals("07040000") || material["Number"].Equals("07040004"))
                                        {
                                            this.Model.SetValue("FDestStockId", BasicObject1, i);
                                            this.Model.SetValue("FSRCSTOCKID", BasicObject, i);
                                        }
                                    }

                                }



                            }
                        }



                    }


                }


                #endregion

            }
        }
    }
}
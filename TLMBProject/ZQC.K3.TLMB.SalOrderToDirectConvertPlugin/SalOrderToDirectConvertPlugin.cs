using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Core;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Metadata.FieldElement;

namespace ZQC.K3.TLMB.SalOrderToDirectConvertPlugin
{
    [Description("销售订单-直接调拨单，分析箱套种类计算箱套数量")]
    //工厂配货单据要根据配货的商品，分析承载商品的箱套类型，并根据配送商品的数量计算需要的箱套的数量。
    //开发单据转换插件，在销售订单-直接调拨单（分录合并）单据转换执行时，分析参与转换的物料需要的箱套种类，并且计算需要的箱套数量。自动新增直接调拨单分录行，将结果填写到工厂配货的直接调拨单中
    public class SalOrderToDirectConvertPlugin : AbstractConvertPlugIn
    {
        /// <summary>
        /// 主单据体的字段携带完毕，与源单的关联关系创建好之后，触发此事件
        /// </summary>
        /// <param name="e"></param>
        public override void OnAfterCreateLink(CreateLinkEventArgs e)
        {
            base.OnAfterCreateLink(e);
            //源单  销售订单明细
            Entity srcFirstEntity = e.SourceBusinessInfo.GetEntity("FSaleOrderEntry");

            //目标单  直接调拨单明细
            Entity mainEntity = e.TargetBusinessInfo.GetEntity("FBillEntry");
            
            // 目标单关联子单据体
            Entity linkEntity = null;
            Form form = e.TargetBusinessInfo.GetForm();
            if (form.LinkSet != null
                && form.LinkSet.LinkEntitys != null
                && form.LinkSet.LinkEntitys.Count != 0)
            {
                linkEntity = e.TargetBusinessInfo.GetEntity(
                    form.LinkSet.LinkEntitys[0].Key);
            }

            if (linkEntity == null)
            {
                return;
            }
            // 获取生成的全部下游单据
            ExtendedDataEntity[] billDataEntitys = e.TargetExtendedDataEntities.FindByEntityKey("FBillHead");

            // 对下游单据，逐张单据进行处理
            foreach (var item in billDataEntitys)
            {
                DynamicObjectCollection directEntry = item["TransferDirectEntry"] as DynamicObjectCollection;
                //合并之后的分录  为了不遍历每次增加
                DynamicObject entryEnd = directEntry[0] as DynamicObject;
                //获取目标单组织 sql中使用
                DynamicObject org = item["StockOrgId"] as DynamicObject;
                long orgId=Convert.ToInt64(org["Id"]);
                decimal slamount = 0;//塑料箱总数
                decimal pmamount = 0;//泡沫箱总数
                for (int i=0;i< directEntry.Count; i++)
                {
                    //目标单分录  
                    DynamicObject entry= directEntry[i] as DynamicObject;
                    //目标单物料
                    DynamicObject material = entry["MaterialId"] as DynamicObject;
                    //目标单调拨数量
                    decimal qty= Convert.ToDecimal(entry["QTY"]);
                    //物料获取装箱数
                    decimal packQty= Convert.ToDecimal(material["FPACKQTY"]);
                    //物料获取箱套类型
                    DynamicObjectCollection materialBase = material["MaterialBase"] as DynamicObjectCollection;
                    foreach (DynamicObject colourAll in materialBase)
                    {
                        string colour = Convert.ToString(colourAll["FBOXCOLOUR"]);
                        if (colour.Equals("1"))//塑料箱
                        {
                            decimal slxAmount = Math.Ceiling(qty / packQty);
                            slamount = slamount + slxAmount;
                        }
                        if (colour.Equals("2"))//泡沫箱
                        {
                            decimal pmxAmount = Math.Ceiling(qty / packQty);
                            pmamount = pmamount + pmxAmount;
                        }
                    }
                   
                }
                //新增分录  判断两种箱套数量是否为空
                if (slamount != 0)
                {
                    // 目标单添加新行，并接受源单字段值
                    DynamicObject newRow = new DynamicObject(mainEntity.DynamicObjectType);
                    directEntry.Add(newRow);
                    //根据组织查询对应 箱套
                    string sql = string.Format(@"select fmaterialid from t_bd_material where fnumber='07040000' and fuseorgid={0}", orgId);
                    long result = DBUtils.ExecuteScalar<long>(base.Context, sql, -1, null);
                    //调入货主类型 string
                    string ownerType = Convert.ToString(item["OwnerTypeIdHead"]);
                    //调出货主类型 string
                    string ownerOutType = Convert.ToString(item["OwnerTypeOutIdHead"]);
                    //调入货主 DynamicObject
                    DynamicObject owner = item["OwnerIdHead"] as DynamicObject;
                    //调出货主 DynamicObject
                    DynamicObject ownerOut = item["OwnerOutIdHead"] as DynamicObject;
                    //调入保管者类型
                    string keeperType = item["OwnerTypeIdHead"] as string;
                    //调出保管者类型
                    string keeperOutType = entryEnd["KeeperTypeId"] as string;
                    //调入保管者 
                    DynamicObject keeper = entryEnd["KeeperId"] as DynamicObject;
                    //调出保管者
                    DynamicObject keeperOut = entryEnd["KeeperOutId"] as DynamicObject;
                    //调拨数量基本单位  BaseQty
                    decimal baseQty = Convert.ToDecimal( entryEnd["BaseQty"]); 

                    //组织基础资料对象
                    if (result !=0 && result != -1)
                    {
                        IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
                        IViewService view = ServiceHelper.GetService<IViewService>();//界面服务
                        FormMetadata Meta = metaService.Load(base.Context, "BD_MATERIAL") as FormMetadata;//获取基础资料元数据
                        DynamicObject BasicObject = view.LoadSingle(base.Context, result, Meta.BusinessInfo.GetDynamicObjectType());
                        //物料ID
                        long materialId = Convert.ToInt64(BasicObject["Id"]);
                        // 填写字段值
                        DynamicObjectCollection materialStock = BasicObject["MaterialStock"] as DynamicObjectCollection;
                        DynamicObjectCollection materialBase = BasicObject["MaterialBase"] as DynamicObjectCollection;

                        //基本单位
                        foreach(DynamicObject base2 in materialBase)
                        {
                            DynamicObject baseUnit = base2["BaseUnitId"] as DynamicObject;
                            newRow["BaseUnitId"] =baseUnit;
                        }
                        //单位  物料   
                        foreach (DynamicObject base1 in materialStock)
                        {
                            DynamicObject unit = base1["StoreUnitID"] as DynamicObject;
                            long unitId = Convert.ToInt64(unit["Id"]);
                            newRow["MaterialId"] = BasicObject;
                            newRow["MaterialId_Id"] = materialId;
                            newRow["QTY"] = slamount;
                            newRow["UnitId"] = unit;
                            newRow["UnitId_Id"] = unitId;
                            newRow["Seq"] = directEntry.Count;



                            //调入货主
                            newRow["OwnerId"] = owner;
                            //调入货主类型
                            newRow["OwnerTypeId"] = ownerType;
                            //调处货主
                            newRow["FOwnerOutId"] = ownerOut;
                            //调出货主类型
                            newRow["OwnerTypeOutId"] = ownerOutType;
                            //调入保管者类型
                            newRow["KeeperTypeId"] = keeperOutType;
                            //调出保管者类型
                            newRow["KeeperTypeOutId"] = keeperOutType;
                            //调入保管者
                            newRow["KeeperId"] = owner;
                            //调出保管者
                            newRow["KeeperOutId"] = ownerOut;
                            //调出数量（基本单位）
                            newRow["BaseQty"] = baseQty;
                        }

                    }

                }





                if(pmamount != 0)
                {
                    // 目标单添加新行
                    DynamicObject newRow = new DynamicObject(entryEnd.DynamicObjectType);
                    directEntry.Add(newRow);
                    //根据组织查询对应 箱套
                    string sql = string.Format(@"select fmaterialid from t_bd_material where fnumber='07040004' and fuseorgid={0}", orgId);
                    long result = DBUtils.ExecuteScalar<long>(base.Context, sql, -1, null);
                    //组织基础资料对象
                    if (result != 0 && result != -1)
                    {
                        IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
                        IViewService view = ServiceHelper.GetService<IViewService>();//界面服务
                        FormMetadata Meta = metaService.Load(base.Context, "BD_MATERIAL") as FormMetadata;//获取基础资料元数据
                        DynamicObject BasicObject = view.LoadSingle(base.Context, result, Meta.BusinessInfo.GetDynamicObjectType());
                        //物料ID
                        long materialId = Convert.ToInt64(BasicObject["Id"]);
                        // 填写字段值
                        DynamicObjectCollection materialStock =  BasicObject["MaterialStock"] as DynamicObjectCollection;
                        foreach(DynamicObject base1 in materialStock)
                        {
                            DynamicObject unit = base1["StoreUnitID"] as DynamicObject;
                            long unitId = Convert.ToInt64(unit["Id"]);
                            newRow["MaterialId"] = BasicObject;
                            newRow["MaterialId_Id"] = materialId;
                            newRow["QTY"] = pmamount;
                            newRow["UnitId"] = unit;
                            newRow["UnitId_Id"] = unitId;
                            newRow["Seq"] = directEntry.Count;
                        }
                        
                       
                    }

                }


                // TODO: 逐个填写其他字段值，示例代码略

            }
        }
    }
}

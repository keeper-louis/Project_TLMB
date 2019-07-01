using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ControlElement;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.TLMB.ZJDBFXZPLXG.DynamicObjectPlugIn
{
    [Description("直接调拨单分销站批量修改")]
    public class ZJDBDFXBillEdit: AbstractDynamicFormPlugIn
    {

        #region 参数
        private BusinessInfo _currInfo = null;
        private LayoutInfo _currLayout = null;
        int jl = 0;
        string tempTable01 = string.Empty;
        string ZT = string.Empty;
        string ZTZ = string.Empty;
        List<ControlAppearance> controls = new List<ControlAppearance>();
        DynamicObjectCollection source1;
        DynamicObjectCollection source;
        ////////////////////////////////
        FormMetadata meta = null;
        BusinessInfo info = null;
        Form form = null;
        BillOpenParameter param = null;
        IResourceServiceProvider provider = null;
        IDynamicFormView service = null;
        /////////////////////////////////
        BusinessInfo bussnessInfo = null;
        IViewService ivs = null;
        DynamicObject billobj = null;
        DynamicObjectCollection billobjEntitys = null;
        BaseDataField ZZInfo = null;
        DynamicObject Entry = null;
        DynamicObject[] ZZObjs = null;
        ////////////////////////////////

        ////////////////////////////////
        int hbdj = 1;
        string deptname = string.Empty;
        long lineId = 0;
        string linename = string.Empty;
        DateTime date = DateTime.Now;
        long orgId = 0;
        long deptId = 0;
        List<string> dgfmid = new List<string>();
        DynamicObjectCollection HQBH = null;
        #endregion

        public override void AfterCreateNewData(EventArgs e)
        {
            base.AfterCreateNewData(e);
            this.getSelectFilter5(this.Context, e);
            meta = MetaDataServiceHelper.Load(this.Context, "STK_TransferDirect") as FormMetadata;
            info = meta.BusinessInfo;
            form = info.GetForm();
            provider = form.GetFormServiceProvider();
            service = provider.GetService(typeof(IDynamicFormView)) as IDynamicFormView;
            param = new BillOpenParameter(form.Id, meta.GetLayoutInfo().Id);
            param.SetCustomParameter("formID", form.Id);
            //param.SetCustomParameter("status", "EDIT");
            param.SetCustomParameter("PlugIns", form.CreateFormPlugIns());
            param.Context = this.Context;
            param.FormMetaData = meta;
            param.LayoutId = meta.GetLayoutInfo().Id;
            param.Status = OperationStatus.EDIT;
            //(service as IBillView).OpenParameter.Status = OperationStatus.EDIT;
            //(service as IBillViewService).LoadData();
            /////////////////////////
            bussnessInfo = MetaDataServiceHelper.GetFormMetaData(this.Context, "PAEZ_PLXGJL").BusinessInfo;
            ivs = Kingdee.BOS.App.ServiceHelper.GetService<IViewService>();
            //billobj = bussnessInfo.GetDynamicObjectType().CreateInstance() as DynamicObject;
            //billobjEntitys = billobj["FEntity"] as DynamicObjectCollection;
            ZZInfo = bussnessInfo.GetField("F_PAEZ_ORGID") as BaseDataField;
        }

        public override void AfterBindData(EventArgs e)
        {
            this.View.GetControl("F_JF_Combo").Visible = false;
            for (int i = 0; i < source1.Count; i++)
            {
                if (source1[i]["fname"].ToString() == "业务主管")
                {
                    this.View.GetControl("FDEPT").Visible = false;
                    break;
                }
            }

            if (source.Count > 1)
            {
                ComboFieldEditor headComboEidtor = this.View.GetControl<ComboFieldEditor>("F_JF_Combo");
                List<EnumItem> comboOptions = new List<EnumItem>();
                for (int i = 0; i < source.Count; i++)
                {
                    comboOptions.Add(new EnumItem() { EnumId = "'" + i + "'", Value = source[i]["fdeptid"].ToString(), Caption = new LocaleValue(source[i]["fname"].ToString()) });
                }
                headComboEidtor.SetComboItems(comboOptions);
                this.View.GetControl("F_JF_Combo").Visible = true;
                this.View.GetControl("FDEPT").Visible = false;
            }
            else if (source.Count == 1)
            {
                this.View.LockField("FDEPT", false);
                this.View.GetControl("F_JF_Combo").Visible = false;
                this.View.GetControl("FDEPT").Visible = true;
            }

            source1 = null;
            source = null;

        }

        private void getSelectFilter5(Context cxt, EventArgs e)
        {
            long userId = Convert.ToInt64(cxt.UserId);//用户ID
            DynamicObject orgObj = this.View.Model.GetValue("FOrgId") as DynamicObject;
            long orgId = Convert.ToInt64(orgObj["Id"]);
            string strSql1 = string.Format(@"/*oracle*/select d.fname
                                              from t_Sec_User a
                                             inner join t_sec_userorg b
                                                on a.fuserid = b.fuserid
                                               and a.FUSERID ={0}
                                             inner join t_sec_userrolemap c
                                                on b.fentityid = c.fentityid
                                             inner join t_Sec_Role_l d
                                                on c.froleid=d.froleid  
                                                and b.forgid={1}", userId, orgId);
            source1 = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql1);

            string strSql = string.Format(@"/*oracle*/select c.fdeptid,dl.fname from T_SEC_user a   
left join  t_hr_empinfo b on  a.flinkobject=b.fpersonid
left join T_BD_STAFFTEMP c on c.fid=b.fid inner join t_bd_department_l dl on dl.fdeptid=c.fdeptid
                              where a.FUSERID ={0}
                              and c.fdeptid in (
select dl.fdeptid from t_bd_department_l dl inner join t_bd_department d on d.fdeptid=dl.fdeptid where dl.fname like '%分销站%' and  d.fuseorgid={1}
)", userId, orgId);
            source = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
            if (source.Count >= 1)
            {
                this.Model.SetValue("FDEPT", source[0]["fdeptid"]);
            }
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            string strkey = e.Field.Key.ToUpper();
            switch (strkey)
            {
                case "F_JF_COMBO":
                    {
                        ComboFieldEditor headComboEidtor = this.View.GetControl<ComboFieldEditor>("F_JF_Combo");
                        string s = this.Model.GetValue("F_JF_Combo").ToString();
                        //this.Model.SetValue("FDEPT", headComboEidtor);
                        Field field = this.View.BusinessInfo.GetField("F_JF_Combo");
                        string b = "";
                        //转为ComboField
                        ComboField comboField = field as ComboField;
                        b = comboField.GetEnumItemName(s);
                        this.Model.SetValue("FDEPT", b);
                        break;
                    }
                case "FDEPT":
                    {
                        this.View.Model.SetValue("FDJZT", "");
                        break;
                    }
                case "FDJZT":
                    {
                        if (jl == 0)
                        {
                            ZTZ = this.View.Model.GetValue("FDJZT").ToString();
                            if (ZTZ != "")
                            {
                                switch (ZTZ)
                                {
                                    case "1":
                                        ZT = "and a.fdocumentstatus <> 'B' and a.fdocumentstatus <> 'C' and a.fdocumentstatus <> 'Z'";
                                        break;
                                    case "2":
                                        ZT = "and a.fdocumentstatus = 'B'";
                                        break;
                                    case "3":
                                        ZT = "and a.fdocumentstatus = 'C'";
                                        break;
                                    default:
                                        ZT = string.Empty;
                                        break;
                                }
                                this.getBillChange();
                            }
                        }
                        break;
                    }
                default:
                    break;
            }
        }



        #region 按钮操作
        /// <summary>  按钮操作 
        /// 
        /// </summary>
        /// <param name="e"></param>
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);


            if (e.Key.EqualsIgnoreCase("FCX"))
            {
                ZTZ = this.View.Model.GetValue("FDJZT").ToString();
                if (ZTZ != "")
                {
                    this.getBillChange();
                }
                else
                {
                    this.View.ShowMessage("请选择状态查询！！！");
                }
            }

            else if (e.Key.EqualsIgnoreCase("FSAVE"))
            {
                //点击保存操作
                ZTZ = this.View.Model.GetValue("FDJZT").ToString();
                switch (ZTZ)
                {
                    case "1":
                        this.UpdateDDData();
                        break;
                    case "0":
                        this.UpdateDDData();
                        break;
                    default:
                        //this.getBillChange();
                        this.View.ShowMessage("此模式下不可修改数据！！！");
                        this.getBillChange();
                        //throw new KDException("异常", "此模式下不可修改数据！！！");
                        break;
                }
            }
            else if (e.Key.EqualsIgnoreCase("Ftransform"))
            {
                //this.NumberChange();

            }
        }
        #endregion

        #region 更新直接调拨单、留存记录


        #region 保存按钮
        /// <summary> 点击保存操作
        /// 
        /// </summary>
        //更新
        List<List<List<string>>> CL = new List<List<List<string>>>(); List<Object> DELETE2 = new List<object>();
        List<Object> DELETE = new List<object>(); List<DynamicObject> SAVE = new List<DynamicObject>();
        List<DynamicObject> SAVE1 = new List<DynamicObject>(); List<DynamicObject> DELETE1 = new List<DynamicObject>();
        private void UpdateDDData()
        {
            CL.Clear(); DELETE.Clear(); SAVE.Clear(); SAVE1.Clear(); DELETE2.Clear(); DELETE1.Clear();

            date = Convert.ToDateTime(this.View.Model.GetValue("FDate"));
            DynamicObject orgObj = this.View.Model.GetValue("FOrgId") as DynamicObject;
            orgId = Convert.ToInt64(orgObj["Id"]);
            DynamicObject deptObj = this.View.Model.GetValue("FDEPT") as DynamicObject;
            deptname = Convert.ToString(deptObj["Name"]);
            deptId = Convert.ToInt64(deptObj["Id"]);
            string sqlStr = string.Format(@"/*oracle*/select distinct e.fid, e.fname  from T_STK_STKTRANSFERIN a
                                          inner join T_TL_LINE_l e
                                          on e.fid = a.fline and a.fdocumentstatus <> 'C' and a.fdocumentstatus <> 'B' and a.FCANCELSTATUS='A'
                                          and e.flocaleid = 2052 
                                          inner join t_bd_department_l c
                                          on c.fdeptid =a.fsaledeptid
                                          and c.flocaleid = 2052 
                                          inner join T_STK_STKTRANSFERINentry f
                                          on f.fid = a.fid 
                                          where a.FPICKDATE = to_date('{0}','yyyy-mm-dd')
                                          and c.fdeptid = {1} and a.FSALEORGID = {2} 
                                          order by e.fname", date.ToString("yyyy-MM-dd"), deptId, orgId);
            DynamicObjectCollection col = DBUtils.ExecuteDynamicObject(this.Context, sqlStr);


            int oldCount = this.Model.GetEntryRowCount("FEntity");
            for (int j = 0; j < col.Count; j++)
            {
                List<List<string>> Line = new List<List<string>>();
                for (int i = oldCount - 2; i >= 0; i--)
                {
                    List<string> WL = new List<string>();
                    long materialId = Convert.ToInt64(this.View.Model.GetValue("FmatialId", i));
                    string materialnam = this.View.Model.GetValue("FmatialNam", i).ToString();

                    decimal changefrom = Math.Round(Convert.ToDecimal(this.View.Model.GetValue(string.Format("F{0}", Convert.ToString(col[j]["fid"])), i)), 2);
                    decimal changeto = Math.Round(Convert.ToDecimal(this.View.Model.GetValue(string.Format("F{0}To", Convert.ToString(col[j]["fid"])), i)), 2);

                    decimal change = changeto - changefrom;
                    lineId = Convert.ToInt64(col[j]["fid"]);
                    //判断是否进行修改

                    //改   往多里改
                    if (changefrom != 0 && changeto != 0 && change > 0)
                    {
                        //this.changDD(materialId, change, lineId, date, orgId, deptId, changefrom, changeto);
                        WL.Add(materialId.ToString()); WL.Add(change.ToString()); WL.Add(changefrom.ToString()); WL.Add(changeto.ToString());
                        WL.Add("0"); Line.Add(WL); continue;
                    }

                    //改   往少里改
                    else if (changefrom != 0 && changeto != 0 && change < 0)
                    {
                        //this.changDD_reduce(materialId, change, lineId, date, orgId, deptId, changefrom, changeto);
                        WL.Add(materialId.ToString()); WL.Add(change.ToString()); WL.Add(changefrom.ToString()); WL.Add(changeto.ToString());
                        WL.Add("1"); Line.Add(WL); continue;
                    }

                    //增     此段代码没有运行
                    else if (changefrom == 0 && changeto != 0)
                    {
                        //this.View.ShowNotificationMessage("温馨提示", "不支持添加单据。"); 
                        //this.insDD(materialId, change, lineId, date, orgId, deptId, changefrom, changeto);
                        WL.Add(materialId.ToString()); WL.Add(change.ToString()); WL.Add(changefrom.ToString()); WL.Add(changeto.ToString());
                        WL.Add("3"); WL.Add(materialnam); Line.Add(WL); continue;
                    }

                    //删
                    else if (changefrom != 0 && changeto == 0)
                    {
                        //this.delDD(materialId, change, lineId, date, orgId, deptId, changefrom, changeto);
                        WL.Add(materialId.ToString()); WL.Add(change.ToString()); WL.Add(changefrom.ToString()); WL.Add(changeto.ToString());
                        WL.Add("2"); Line.Add(WL); continue;
                    }
                }
                CL.Add(Line);
            }

            List<string> FZJWL = new List<string>(); string s1 = string.Empty;
            List<List<string>> ZJ = new List<List<string>>();
            List<List<string>> FZJ = new List<List<string>>();
            for (int i = 0; i < CL.Count; i++)
            {
                ZJ.Clear(); FZJ.Clear(); dgfmid.Clear(); FZJWL.Clear(); s1 = string.Empty;
                lineId = Convert.ToInt64(col[i]["fid"]); linename = col[i]["fname"].ToString();
                //cl[i]是否为空
                for (int j = 0; j < CL[i].Count; j++)
                {
                    if (CL[i][j][4] == "3")
                    {
                        ZJ.Add(CL[i][j]);
                        continue;
                    }
                    else if (CL[i][j][4] == "1")
                    {
                        dgfmid.Add(CL[i][j][0]);
                        FZJWL.Add(CL[i][j][0]);
                        FZJ.Add(CL[i][j]);
                        continue;
                    }
                    else
                    {
                        FZJWL.Add(CL[i][j][0]);
                        FZJ.Add(CL[i][j]);
                        continue;
                    }
                }
                if (FZJWL.Count != 0)
                {
                    s1 = string.Format(" ({0}) ", string.Join(",", FZJWL));
                }
                if (ZJ.Count != 0 || FZJ.Count != 0)
                {
                    ZL(ZJ, FZJ, s1);
                }
            }

            //if (SAVE.Count != 0)
            //{
            //    DynamicObject[] a = BusinessDataServiceHelper.Save(this.Context, SAVE.ToArray());
            //    if (a.Length != 0)
            //    {

            if (DELETE2.Count != 0)
            {
                string s = string.Empty;
                DELETE2 = DELETE2.Distinct<object>().ToList();
                for (int i = 0; i < DELETE2.Count; i++)
                {
                    s = string.Format("({0})", string.Join(",", DELETE2));
                }
                string sqlstr = string.Format(@"/*oracle*/ select a.fid,a.fbillno from T_STK_STKTRANSFERIN a
                                                                     left join T_STK_STKTRANSFERINentry b on a.fid = b.fid
                                                                   where a.fid in {0}
                                                                     group by a.fid,a.fbillno having count(fentryid)=0", s);
                DynamicObjectCollection col1 = DBUtils.ExecuteDynamicObject(this.Context, sqlstr);
                DELETE2.Clear();
                if (col1.Count != 0)
                {
                    for (int i = 0; i < col1.Count; i++)
                    {
                        List<string> l = new List<string>();
                        l.Add(col1[i]["fbillno"].ToString());
                        DELETE.Add(col1[i]["fid"]);
                        this.EffectSalOrderRecordupdate(l, 0);
                    }
                }

                if (DELETE.Count != 0)
                {
                    DELETE = DELETE.Distinct<object>().ToList<object>();
                    IOperationResult b = BusinessDataServiceHelper.Delete(this.Context, info, DELETE.ToArray());
                    if (b.IsSuccess)
                    {
                        BusinessDataServiceHelper.Save(this.Context, bussnessInfo, DELETE1[DELETE1.Count - 1]);
                        hbdj = 1;
                    }
                }
                //else
                //{
                //    if (SAVE1.Count != 0)
                //    {
                //        IOperationResult or = BusinessDataServiceHelper.Save(this.Context, bussnessInfo, SAVE1[SAVE1.Count - 1]);
                //    }//SAVE1.ToArray()
                //}
                //    }


                //}

            }

            this.getBillChange();
            DateTime end = DateTime.Now;
        }

        List<List<string>> DG = new List<List<string>>();
        List<List<string>> SG = new List<List<string>>();
        List<List<string>> SC = new List<List<string>>();
        private void ZL(List<List<string>> ZJ, List<List<string>> FZJ, string s)
        {
            DG.Clear(); SG.Clear(); SC.Clear();
            DynamicObjectCollection col = null;
            if (FZJ.Count > 0)
            {
                string sqlstr = string.Format(@"/*oracle*/select a.fbillno,a.fid,f.fentryid,mat.fmaterialid,mat.fname mfname,c.fdeptid,c.fname dfname,f.fqty,matl.fpackqty 
                                            from T_STK_STKTRANSFERIN a inner join T_TL_LINE_l e on e.fid = a.fline and e.flocaleid = 2052
                                            inner join t_bd_department_l c on c.fdeptid = a.fsaledeptid and c.flocaleid = 2052
                                            inner join T_STK_STKTRANSFERINentry f on f.fid = a.fid
                                            inner join t_bd_material_l mat on mat.fmaterialid = f.fmaterialid and mat.flocaleid = 2052
                                            inner join t_bd_material matl on matl.fmaterialid = f.fmaterialid
                                          where a.FPICKDATE = to_date('{0}','yyyy-mm-dd')
                                          and c.fdeptid = {1} and a.FSALEORGID = {2}  
                                          and a.fdocumentstatus <> 'C' and a.fdocumentstatus <> 'B' and a.fdocumentstatus <> 'Z' and a.FCANCELSTATUS='A' and e.fid = {3}
                                          and mat.fmaterialid in {4} order by mat.fmaterialid,a.fbillno
                                          ", date.ToString("yyyy-MM-dd"), deptId, orgId, lineId, s);
                col = DBUtils.ExecuteDynamicObject(this.Context, sqlstr);
                if (col.Count > 0)
                {
                    string sqlstr1 = string.Format(@"/*oracle*/select a.fbillno from T_STK_STKTRANSFERIN a inner join T_TL_LINE_l e 
                                            on e.fid = a.fline and e.flocaleid = 2052
                                            inner join t_bd_department_l c on c.fdeptid = a.fsaledeptid and c.flocaleid = 2052
                                            inner join T_STK_STKTRANSFERINentry f on f.fid = a.fid
                                            inner join t_bd_material_l mat on mat.fmaterialid = f.fmaterialid and mat.flocaleid = 2052
                                            inner join t_bd_material matl on matl.fmaterialid = f.fmaterialid
                                          where a.FPICKDATE = to_date('{0}','yyyy-mm-dd')
                                          and c.fdeptid = {1} and a.FSALEORGID = {2}  
                                          and a.fdocumentstatus <> 'C' and a.fdocumentstatus <> 'B' and a.fdocumentstatus <> 'Z' 
                                          and a.FCANCELSTATUS='A' and e.fid = {3} group by a.fbillno
                                            order by a.fbillno
                                          ", date.ToString("yyyy-MM-dd"), deptId, orgId, lineId);
                    HQBH = DBUtils.ExecuteDynamicObject(this.Context, sqlstr1);
                    //List<string> DEPTIds1 = col.Select(o => o["FEDPET_Id"].ToString()).ToList().Distinct<string>();
                    for (int i = 0; i < col.Count; i++)
                    {
                        List<string> tn = new List<string>();
                        for (int j = 0; j < FZJ.Count; j++)
                        {
                            if (Convert.ToInt32(col[i]["fmaterialid"]) == Convert.ToInt32(FZJ[j][0]))
                            {
                                if (FZJ[j][4].ToString() == "0")
                                {
                                    tn.Add(FZJ[j][0]); tn.Add(FZJ[j][1]); tn.Add(FZJ[j][2]); tn.Add(FZJ[j][3]); tn.Add(FZJ[j][4]); tn.Add(col[i]["fbillno"].ToString()); tn.Add(col[i]["fid"].ToString()); tn.Add(col[i]["fentryid"].ToString());
                                    tn.Add(col[i]["fmaterialid"].ToString()); tn.Add(col[i]["mfname"].ToString()); tn.Add(col[i]["fdeptid"].ToString());
                                    tn.Add(col[i]["dfname"].ToString()); tn.Add(col[i]["fqty"].ToString()); tn.Add(col[i]["fpackqty"].ToString());
                                    DG.Add(tn);
                                    break;
                                }
                                else if (FZJ[j][4].ToString() == "1")
                                {
                                    tn.Add(FZJ[j][0]); tn.Add(FZJ[j][1]); tn.Add(FZJ[j][2]); tn.Add(FZJ[j][3]); tn.Add(FZJ[j][4]); tn.Add(col[i]["fbillno"].ToString()); tn.Add(col[i]["fid"].ToString()); tn.Add(col[i]["fentryid"].ToString());
                                    tn.Add(col[i]["fmaterialid"].ToString()); tn.Add(col[i]["mfname"].ToString()); tn.Add(col[i]["fdeptid"].ToString());
                                    tn.Add(col[i]["dfname"].ToString()); tn.Add(col[i]["fqty"].ToString()); tn.Add(col[i]["fpackqty"].ToString());
                                    SG.Add(tn);
                                    break;
                                }
                                else if (FZJ[j][4].ToString() == "2")
                                {
                                    tn.Add(FZJ[j][0]); tn.Add(FZJ[j][1]); tn.Add(FZJ[j][2]); tn.Add(FZJ[j][3]); tn.Add(FZJ[j][4]); tn.Add(col[i]["fbillno"].ToString()); tn.Add(col[i]["fid"].ToString()); tn.Add(col[i]["fentryid"].ToString());
                                    tn.Add(col[i]["fmaterialid"].ToString()); tn.Add(col[i]["mfname"].ToString()); tn.Add(col[i]["fdeptid"].ToString());
                                    tn.Add(col[i]["dfname"].ToString()); tn.Add(col[i]["fqty"].ToString()); tn.Add(col[i]["fpackqty"].ToString());
                                    SC.Add(tn);
                                    break;
                                }

                            }
                        }
                    }
                    CL1(ZJ, DG, SG, SC, null);
                }
            }
            else
            {
                string sqlstr = string.Format(@"/*oracle*/select min(a.fbillno) fbillno,min(a.fid) fid from T_STK_STKTRANSFERIN a inner join T_TL_LINE_l e 
                                            on e.fid = a.fline and e.flocaleid = 2052
                                            inner join t_bd_department_l c on c.fdeptid = a.fsaledeptid and c.flocaleid = 2052
                                            inner join T_STK_STKTRANSFERINentry f on f.fid = a.fid
                                            inner join t_bd_material_l mat on mat.fmaterialid = f.fmaterialid and mat.flocaleid = 2052
                                            inner join t_bd_material matl on matl.fmaterialid = f.fmaterialid
                                          where a.FPICKDATE = to_date('{0}','yyyy-mm-dd')
                                          and c.fdeptid = {1} and a.FSALEORGID = {2}  
                                          and a.fdocumentstatus <> 'C' and a.fdocumentstatus <> 'B' and a.fdocumentstatus <> 'Z'
                                          and a.FCANCELSTATUS='A' and e.fid = {3} group by a.fbillno,a.fid
                                            order by a.fbillno
                                          ", date.ToString("yyyy-MM-dd"), deptId, orgId, lineId);
                col = DBUtils.ExecuteDynamicObject(this.Context, sqlstr);
                if (col.Count > 0)
                {
                    CL1(ZJ, null, null, null, col);
                }
            }
        }

        List<List<string>> SG2 = new List<List<string>>();
        List<List<List<string>>> SG1 = new List<List<List<string>>>(); List<List<List<string>>> ZZZL = new List<List<List<string>>>();
        private void CL1(List<List<string>> ZJ, List<List<string>> DG, List<List<string>> SG, List<List<string>> SC, DynamicObjectCollection col)
        {
            SG2.Clear(); SG1.Clear(); ZZZL.Clear();
            if (DG == null && SG == null && SC == null)
            {
                param.PkValue = Convert.ToInt64(col[0]["fid"]); DELETE2.Add(param.PkValue);
                (service as IBillViewService).Initialize(param, provider);
                (service as IBillViewService).LoadData();
                if ((service as IBillView).Model.DataObject == null)
                {
                    return;
                }
                try
                {
                    DynamicObjectCollection col1 = (service as IBillView).Model.DataObject["TransferDirectEntry"] as DynamicObjectCollection;
                    int row = col1.Count;
                    if (row > 0)
                    {
                        for (int j = 0; j < ZJ.Count; j++)
                        {
                            ZJ[j].Add(col[0]["fbillno"].ToString());
                            ZJFL(col1, row + j, Convert.ToDecimal(ZJ[j][3]), ZJ[j]);
                        }
                    }
                    //SAVE.Add((service as IBillView).Model.DataObject);
                    (service as IBillView).InvokeFormOperation(FormOperationEnum.Save);
                    (service as IBillView).Close();
                    IOperationResult result = BusinessDataServiceHelper.Save(this.Context, info, (service as IBillView).Model.DataObject);
                    if (result.IsSuccess)
                    {
                        IOperationResult or = BusinessDataServiceHelper.Save(this.Context, bussnessInfo, SAVE1[SAVE1.Count - 1]);
                        SAVE1.Clear(); hbdj = 1;
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            else
            {
                if (DG.Count != 0)
                {
                    DG = DG.Where((x, i) => DG.FindIndex(z => z[0] == x[0]) == i).ToList();
                }
                if (SG.Count != 0)
                {
                    for (int i = 0; i < dgfmid.Count; i++)
                    {
                        List<List<string>> temp1 = new List<List<string>>();
                        for (int j = 0; j < SG.Count; j++)
                        {
                            if (dgfmid[i] == SG[j][0])
                            {
                                List<string> temp = new List<string>();
                                temp = SG[j];
                                //List<string> temp2 = new List<string>();
                                temp1.Add(temp);
                                continue;
                            }
                        }
                        if (temp1.Count != 0)
                        {
                            SG1.Add(temp1);
                        }
                    }
                    decimal Rest = 0;
                    for (int i = 0; i < SG1.Count; i++)
                    {
                        Rest = Convert.ToDecimal(SG1[i][0][1]);
                        for (int j = 0; j < SG1[i].Count; j++)
                        {
                            decimal FRealQty = Convert.ToDecimal(SG1[i][j][12]);//库里值
                            if (Rest + FRealQty <= 0)
                            {
                                List<string> temp = new List<string>();
                                SG1[i][j][3] = "0"; SG1[i][j][4] = "2";
                                temp = SG1[i][j];
                                SG2.Add(temp);
                            }
                            else if (Rest + FRealQty > 0 && Rest < 0)
                            {
                                List<string> temp = new List<string>();
                                SG1[i][j][1] = (Rest).ToString(); SG1[i][j][3] = (Rest + FRealQty).ToString(); SG1[i][j][4] = "0";
                                temp = SG1[i][j];
                                SG2.Add(temp);
                            }
                            Rest = Rest + FRealQty;
                        }
                    }

                }


                for (int i = 0; i < HQBH.Count; i++)
                {
                    List<List<string>> ZL = new List<List<string>>();
                    for (int j = 0; j < SG2.Count; j++)
                    {
                        if (HQBH[i]["fbillno"].ToString() == SG2[j][5])
                        {
                            List<string> temp = new List<string>();
                            temp = SG2[j];
                            ZL.Add(temp);
                            continue;
                        }
                    }
                    for (int k = 0; k < SC.Count; k++)
                    {
                        if (HQBH[i]["fbillno"].ToString() == SC[k][5])
                        {
                            List<string> temp = new List<string>();
                            temp = SC[k];
                            ZL.Add(temp);
                            continue;
                        }
                    }
                    for (int l = 0; l < DG.Count; l++)
                    {
                        if (HQBH[i]["fbillno"].ToString() == DG[l][5])
                        {
                            List<string> temp = new List<string>();
                            temp = DG[l];
                            ZL.Add(temp);
                            continue;
                        }
                    }
                    if (ZL.Count != 0)
                    {
                        ZZZL.Add(ZL);
                    }

                }

                NEWEffectChangeSalOrder(ZZZL, ZJ);

            }
        }

        private void NEWEffectChangeSalOrder(List<List<List<string>>> ZZZL, List<List<string>> ZJ)
        {
            for (int i = 0; i < ZZZL.Count; i++)
            {
                param.PkValue = Convert.ToInt64(ZZZL[i][0][6]); DELETE2.Add(param.PkValue);
                (service as IBillViewService).Initialize(param, provider);
                (service as IBillViewService).LoadData();
                if ((service as IBillView).Model.DataObject == null)
                {
                    return;
                }
                try
                {
                    DynamicObjectCollection col = (service as IBillView).Model.DataObject["TransferDirectEntry"] as DynamicObjectCollection;
                    if (i == 0)
                    {
                        int row = col.Count;
                        if (row > 0)
                        {
                            for (int j = 0; j < ZJ.Count; j++)
                            {
                                ZJ[j].Add(ZZZL[i][0][5]);
                                ZJFL(col, row + j, Convert.ToDecimal(ZJ[j][3]), ZJ[j]);
                            }
                        }
                    }

                    for (int j = 0; j < ZZZL[i].Count; j++)
                    {
                        for (int k = 0; k < col.Count; k++)
                        {
                            long Id = Convert.ToInt64(col[k]["Id"]);
                            if (Convert.ToInt64(ZZZL[i][j][7]) == Id)
                            {
                                if (ZZZL[i][j][4].ToString() == "2")//删除
                                {
                                    (service as IBillView).Model.DeleteEntryRow("FBillEntry", k);
                                    this.EffectSalOrderRecordupdate(ZZZL[i][j], 1);
                                    break;
                                }
                                else if (ZZZL[i][j][4].ToString() == "0") //修改
                                {
                                    decimal sum = Convert.ToDecimal(ZZZL[i][j][12]) + Convert.ToDecimal(ZZZL[i][j][1]);
                                    decimal packqty = Convert.ToDecimal(ZZZL[i][j][13]);

                                    DynamicObject obj = (service as IBillView).Model.GetValue("FMaterialId", k) as DynamicObject;
                                    DynamicObjectCollection fbase = obj["MaterialBase"] as DynamicObjectCollection;
                                    string colour = fbase[0]["FBOXCOLOUR"].ToString();

                                    (service as IBillView).Model.SetValue("FQty", sum, k);
                                    (service as IBillView).InvokeFieldUpdateService("FQty", k);//更新调拨数量字段
                                    if (packqty == 0)
                                    {
                                        (service as IBillView).Model.SetValue("FPACKERQTY", 1, k);
                                        (service as IBillView).Model.SetValue("FPACKETQTY", 1, k);
                                        (service as IBillView).Model.SetValue("FPACKQTY", packqty, k);
                                        (service as IBillView).Model.SetValue("FLEFTQTY", sum, k);
                                        if (colour == "1")
                                        {
                                            (service as IBillView).Model.SetValue("FSLXQTY", 1, k);
                                        }
                                        if (colour == "2")
                                        {
                                            (service as IBillView).Model.SetValue("FPMXQTY", 1, k);
                                        }
                                    }
                                    else
                                    {
                                        (service as IBillView).Model.SetValue("FPACKERQTY", Math.Ceiling(sum / packqty), k);//箱套数如果有余数,件数+1(向上取整)
                                        (service as IBillView).Model.SetValue("FPACKETQTY", Math.Floor(sum / packqty), k);//件数
                                        (service as IBillView).Model.SetValue("FPACKQTY", sum / packqty, k);//包装数量
                                        (service as IBillView).Model.SetValue("FLEFTQTY", sum - (Math.Floor(sum / packqty) * packqty), k);//余数
                                        if (colour == "1")
                                        {
                                            (service as IBillView).Model.SetValue("FSLXQTY", Math.Ceiling(sum / packqty), k);//塑料箱
                                        }
                                        if (colour == "2")
                                        {
                                            (service as IBillView).Model.SetValue("FPMXQTY", Math.Ceiling(sum / packqty), k);//泡沫箱
                                        }
                                    }


                                    this.EffectSalOrderRecordupdate(ZZZL[i][j], 1);
                                    break;
                                }
                            }
                        }
                    }

                    //SAVE.Add((service as IBillView).Model.DataObject);
                    (service as IBillView).InvokeFormOperation(FormOperationEnum.Save);
                    (service as IBillView).Close();
                    IOperationResult result = BusinessDataServiceHelper.Save(this.Context, info, (service as IBillView).Model.DataObject);
                    if (result.IsSuccess)
                    {
                        IOperationResult or = BusinessDataServiceHelper.Save(this.Context, bussnessInfo, SAVE1[SAVE1.Count - 1]);
                        SAVE1.Clear(); hbdj = 1;
                    }

                }
                catch (Exception e)
                {
                    throw e;
                }

            }
        }

        private void ZJFL(DynamicObjectCollection col1, int row, decimal p2, List<string> zj)
        {
            long materialId = Convert.ToInt64(zj[0]);
            //string sqlStr = string.Format(@"/*oracle*/select fexpperiod from t_BD_MaterialStock where fmaterialid={0} ", materialId);
            ////DynamicObjectCollection colk = DBUtils.ExecuteDynamicObject(this.Context, sqlStr);
            ////int expperiod = Convert.ToInt32(colk[0]["fexpperiod"]);

            // DateTime date0 = Convert.ToDateTime("9990-12-31").AddDays(expperiod);
            (service as IBillView).Model.InsertEntryRow("FBillEntry", row);
            (service as IBillView).Model.SetValue("FMaterialId", materialId, row);

            DynamicObject obj = (service as IBillView).Model.GetValue("FMaterialId", row) as DynamicObject;
            decimal packqty = Convert.ToInt64(obj["FPACKQTY"]);

            decimal sum = p2;

            (service as IBillView).Model.SetValue("FUnitID", col1[0]["UnitID_Id"], row);
            (service as IBillView).Model.SetValue("FBaseUnitId", col1[0]["BaseUnitId_Id"], row);
            (service as IBillView).Model.SetValue("FQty", sum, row);
            (service as IBillView).InvokeFieldUpdateService("FQty", row);
            //(service as IBillView).Model.SetValue("FExpiryDate", date0, row);
            (service as IBillView).Model.SetValue("FPriceQty", sum, row);
            (service as IBillView).Model.SetValue("FBaseQty", sum, row);
            (service as IBillView).Model.SetValue("FPriceBaseQty", sum, row);
            //(service as IBillView).Model.SetValue("FLot", "0", row);
            (service as IBillView).Model.SetValue("FSrcStockId", col1[0]["SrcStockId_Id"], row);
            (service as IBillView).Model.SetValue("FDestStockId", col1[0]["DestStockId_Id"], row);



            //2017-2-9修改,内容对件数新增进行修改
            DynamicObjectCollection fbase = obj["MaterialBase"] as DynamicObjectCollection;
            string colour = fbase[0]["FBOXCOLOUR"].ToString();
            if (packqty == 0)
            {
                (service as IBillView).Model.SetValue("FPACKERQTY", 1, row);
                (service as IBillView).Model.SetValue("FPACKETQTY", 1, row);
                (service as IBillView).Model.SetValue("FPACKQTY", packqty, row);
                (service as IBillView).Model.SetValue("FLEFTQTY", sum, row);
                if (colour == "1")
                {
                    (service as IBillView).Model.SetValue("FSLXQTY", 1, row);
                }
                if (colour == "2")
                {
                    (service as IBillView).Model.SetValue("FPMXQTY", 1, row);
                }
            }
            else
            {
                (service as IBillView).Model.SetValue("FPACKERQTY", Math.Ceiling(sum / packqty), row);
                (service as IBillView).Model.SetValue("FPACKETQTY", Math.Floor(sum / packqty), row);
                (service as IBillView).Model.SetValue("FPACKQTY", sum / packqty, row);
                (service as IBillView).Model.SetValue("FLEFTQTY", sum - (Math.Floor(sum / packqty) * packqty), row);
                if (colour == "1")
                {
                    (service as IBillView).Model.SetValue("FSLXQTY", Math.Ceiling(sum / packqty), row);
                }
                if (colour == "2")
                {
                    (service as IBillView).Model.SetValue("FPMXQTY", Math.Ceiling(sum / packqty), row);
                }
            }
            (service as IBillView).Model.SetValue("FQty", sum, row);
            (service as IBillView).InvokeFieldUpdateService("FQty", row);
            this.EffectSalOrderRecordupdate(zj, 2);
        }

        private void EffectSalOrderRecordupdate(List<string> list, int i)
        {
            if (hbdj == 1)
            //if (true)
            {
                //创建一个单据字段动态对象
                billobj = bussnessInfo.GetDynamicObjectType().CreateInstance() as DynamicObject;
                billobjEntitys = billobj["FEntity"] as DynamicObjectCollection;
                //单据头部分
                ZZInfo.RefIDDynamicProperty.SetValue(billobj, orgId);
                ZZObjs = ivs.LoadFromCache(this.Context, new object[] { orgId }, ZZInfo.RefFormDynamicObjectType);
                if (ZZObjs.Count() > 0)
                    ZZInfo.DynamicProperty.SetValue(billobj, ZZObjs[0]);
                //日期
                billobj["F_PAEZ_Date"] = date;
                hbdj = 0;
                //单据体部分
                Entry = billobjEntitys.DynamicCollectionItemPropertyType.CreateInstance() as DynamicObject;
            }
            else
            {
                //单据体部分
                Entry = billobjEntitys.DynamicCollectionItemPropertyType.CreateInstance() as DynamicObject;
            }
            try
            {
                if (i == 0)
                {
                    //单据号
                    Entry["FXGDJDH"] = list[0];
                    //修改日期
                    DateTime now = DateTime.Now;
                    Entry["FDATE"] = now.ToString("yyyy-MM-dd");
                    //修改人
                    Entry["FXGR"] = Convert.ToString(this.Context.UserName);
                    //修改时间
                    Entry["FDATETIME"] = now.ToString("HH:mm:ss");
                    billobjEntitys.Add(Entry);
                    DELETE1.Add(billobj);
                }
                else if (i == 1)
                {
                    //单据号
                    Entry["FXGDJDH"] = list[5];
                    //物料名
                    Entry["FMATERIAL"] = list[9];
                    //分销站
                    Entry["FDEPT"] = list[11];
                    //修改前
                    Entry["FDECIMALFROM"] = list[12];
                    //修改后
                    if (list[4] == "0")
                    {
                        Entry["FDECIMALTO"] = Convert.ToDecimal(list[12]) + Convert.ToDecimal(list[1]);
                    }
                    else if (list[4] == "2")
                    {
                        Entry["FDECIMALTO"] = 0;
                    }
                    //线路
                    Entry["FLINE"] = linename;
                    //修改日期
                    DateTime now = DateTime.Now;
                    Entry["FDATE"] = now.ToString("yyyy-MM-dd");
                    //修改人
                    Entry["FXGR"] = Convert.ToString(this.Context.UserName);
                    //修改时间
                    Entry["FDATETIME"] = now.ToString("HH:mm:ss");
                    billobjEntitys.Add(Entry);
                    SAVE1.Add(billobj);
                }
                else if (i == 2)
                {
                    //单据号
                    Entry["FXGDJDH"] = list[6];
                    //物料名
                    Entry["FMATERIAL"] = list[5];
                    //分销站
                    Entry["FDEPT"] = deptname;
                    //修改前
                    Entry["FDECIMALFROM"] = 0;
                    //修改后
                    Entry["FDECIMALTO"] = list[3];
                    //线路
                    Entry["FLINE"] = linename;
                    //修改日期
                    DateTime now = DateTime.Now;
                    Entry["FDATE"] = now.ToString("yyyy-MM-dd");
                    //修改人
                    Entry["FXGR"] = Convert.ToString(this.Context.UserName);
                    //修改时间
                    Entry["FDATETIME"] = now.ToString("HH:mm:ss");
                    billobjEntitys.Add(Entry);
                    SAVE1.Add(billobj);
                }
                //IOperationResult SaveResult = BusinessDataServiceHelper.Save(this.Context, bussnessInfo, billobj, null, "Save");
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Entry = null;
                ZZObjs = null;
            }
        }

        #endregion
        #endregion

        #region 十字表形成

        /// <summary>查询触发事件
        /// 
        /// </summary>
        private void getBillChange()
        {
            DateTime date = Convert.ToDateTime(this.View.Model.GetValue("FDate"));
            DynamicObject orgObj = this.View.Model.GetValue("FOrgId") as DynamicObject;
            long orgId = Convert.ToInt64(orgObj["Id"]);
            DynamicObject deptObj = this.View.Model.GetValue("FDEPT") as DynamicObject;
            long deptId = Convert.ToInt64(deptObj["Id"]);
            string sqlstr1 = string.Format(@"select a.fid,b.fname from T_TL_LINE a
                                               inner join T_TL_LINE_l b on a.fid = b.fid
                                               and b.flocaleid = 2052 
                                               where FDEPT = {0}   order by b.fname", deptId);
            DynamicObjectCollection col1 = DBUtils.ExecuteDynamicObject(this.Context, sqlstr1);

            this.getAddColumn(col1);
            EntryGrid grid = this.View.GetControl<EntryGrid>("FEntity");
            grid.SetAllowLayoutSetting(false);  // 列按照索引显示
            EntityAppearance listAppearance = _currLayout.GetEntityAppearance("FEntity");
            grid.CreateDyanmicList(listAppearance);
            this.SetData(date, orgId, deptId, col1);
            this.setFreezeColunm(4, controls, grid);
            this.View.SendDynamicFormAction(this.View);
            this.View.UpdateView("FEntity");
            controls.Clear();
        }

        /// <summary>冻结列
        /// 
        /// </summary>
        /// <param name="leftSize"></param>
        /// <param name="controls"></param>
        /// <param name="entry"></param>
        private void setFreezeColunm(int leftSize, List<ControlAppearance> controls, EntryGrid entry)
        {
            string leftKey = "";
            if (leftSize > 0)
            {
                if (leftSize > controls.Count) return;
                int i = 1;

                foreach (var control in controls)
                {
                    if (control.Visible == 12 || this.View.GetControl(control.Key).Visible == false) continue;
                    if (i == leftSize)
                    {
                        leftKey = control.Key;
                        break;
                    }
                    i++;
                }
            }
            entry.SetFrozen(leftKey.ToUpperInvariant(), "");
        }

        /// <summary>  赋值
        /// 
        /// </summary>
        /// <param name="date"></param>
        /// <param name="orgId"></param>
        /// <param name="deptId"></param>
        /// <param name="col"></param>
        private void SetData(DateTime date, long orgId, long deptId, DynamicObjectCollection col)
        {
            try
            {
                ZTZ = this.View.Model.GetValue("FDJZT").ToString();
                string wlfzgl = string.Empty;
                string wlfz = this.View.Model.GetValue("FWLFL").ToString();
                if (wlfz != "")
                {
                    wlfzgl = string.Format("and FWLFL='{0}'", wlfz);
                }
                else
                {
                    wlfzgl = string.Empty;
                }
                this.tempTable01 = AddTempTable(base.Context);
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat("/*oracle*/create table {0}", tempTable01);
                builder.Append(" (");
                builder.Append("  Fmaterialid int,");
                for (int i = 0; i < col.Count; i++)
                {
                    builder.AppendFormat(" F{0} decimal(23,2) default 0 not null,", Convert.ToString(col[i]["fid"]));
                }
                builder.AppendFormat(" Fhj decimal(23,2) default 0 not null,");
                builder.AppendFormat(" FBs varchar(50),");
                builder.Append("  Fmaterialname varchar(100),");
                builder.Append("  fispack varchar(50)");
                builder.Append("  )");
                DBUtils.Execute(this.Context, builder.ToString());
                builder.Clear();

                builder.AppendFormat("/*oracle*/insert into {0}", tempTable01);
                builder.Append(" (Fmaterialid,");
                for (int i = 0; i < col.Count; i++)
                {
                    builder.AppendFormat(" F{0},", Convert.ToString(col[i]["fid"]));
                }
                builder.Append(" Fhj,");
                builder.Append(" FBs,");
                builder.Append(" Fmaterialname,fispack)");
                builder.Append(" select fmaterialid,");
                for (int i = 0; i < col.Count; i++)
                {
                    builder.AppendFormat(" sum(F{0}),", Convert.ToString(col[i]["fid"]));
                }
                //2017年3月10日14:06:55

                builder.Append(" sum(Fhj),");
                builder.Append(@" (case when sum(FBsY) = 0 and sum(FBsZ) <>0  and fispack=1  then to_char(sum(FBsZ))||'件' 
                          when sum(FBsY) <> 0 and sum(FBsZ) <>0  and fispack=1  then to_char(sum(FBsZ))||'件余'||to_char(sum(FBsY))||'包'
                           when sum(FBsY) <> 0 and sum(FBsZ) = 0   then to_char(sum(FBsY))||'包'
                         
                          else '0件' end),");
                builder.Append(" fmaterialnam,fispack from(");

                builder.Append(" select /*二开-分销站直调交叉表*/ fmaterialid,");
                for (int i = 0; i < col.Count; i++)
                {
                    builder.AppendFormat(" sum(F{0}) F{0},", Convert.ToString(col[i]["fid"]));
                }
                builder.Append(" sum(Fhj) Fhj,");
                builder.Append("    (case when FPACKQTY = 0 then   0  WHEN FPACKQTY != 0 and fispack = 1 then   floor(sum(Fhj) / FPACKQTY)  when FPACKQTY != 0 and fispack = 0 then 0 end) FBsZ,");
                builder.Append("    (case  when FPACKQTY = 0 then     0     WHEN FPACKQTY != 0 and fispack = 1 then     mod(sum(Fhj),");
                builder.Append("    FPACKQTY) when FPACKQTY != 0 and fispack = 0 then  sum(Fhj)   end) FBsY,");
                builder.Append(" fmaterialnam,fispack");

                builder.Append(" from(select mat.fmaterialid,");
                for (int i = 0; i < col.Count; i++)
                {
                    builder.AppendFormat(" (case when e.fname = '{0}' and matl.fispack=1 then sum(f.fqty) when e.fname = '{0}' and matl.fispack=0  then   sum(f.fqty)  else 0 end) F{1} ,", Convert.ToString(col[i]["fname"])
                        , Convert.ToString(col[i]["fid"]));
                }
                builder.Append("  sum(f.fqty) Fhj,");
                builder.Append("  0 FBsZ,");
                builder.Append("  0 FBsY,mat.fname fmaterialnam ,matl.fispack fispack,matl.fpackqty from T_STK_STKTRANSFERIN a");

                builder.Append("  inner join t_bd_department_l c on c.fdeptid = a.fsaledeptid and a.FCANCELSTATUS='A' and c.flocaleid = 2052");
                builder.Append("   inner join T_TL_LINE_l e on e.fid = a.fline and e.flocaleid = 2052  ");
                builder.Append("    inner join T_STK_STKTRANSFERINentry f on f.fid = a.fid  ");
                builder.Append("     inner join t_bd_material_l mat on mat.fmaterialid = f.fmaterialid and mat.flocaleid = 2052  ");
                builder.Append("    inner join t_bd_material matl  on matl.fmaterialid = mat.fmaterialid and mat.flocaleid = 2052 ");
                builder.AppendFormat(" where  a.FPICKDATE = to_date('{0}','yyyy-mm-dd') and matl.fisui='1' {1} {2}", date.ToString("yyyy-MM-dd"), wlfzgl, ZT);
                builder.AppendFormat("  and  a.FALLOCATETYPE=2 and c.fdeptid = {0} and a.FSALEORGID = {1} group by mat.fmaterialid,mat.fname,matl.fpackqty,matl.fispack,e.fname", deptId, orgId);



                //以下是正确的
                builder.Append(" union all ");


                builder.Append(" select mat.fmaterialid,");
                for (int i = 0; i < col.Count; i++)
                {
                    builder.Append(" 0,");
                }

                builder.Append(" 0,0,0,matl.fname,mat.fispack fispack,mat.fpackqty from t_Bd_Material mat");
                builder.Append(" inner join t_bd_material_l matl on mat.fmaterialid = matl.fmaterialid");
                builder.Append(" and matl.flocaleid = 2052");
                builder.Append(" inner join t_Bd_Materialgroup matg on matg.fid = mat.fmaterialgroup");
                builder.Append(" and fparentid = (select fid from t_Bd_Materialgroup where fnumber = '07' )");
                builder.Append(" where matg.fnumber not in (07.03,07.04) and mat.fdocumentstatus = 'C'  and mat.fforbidstatus='A'  and mat.fno!=0 ");
                builder.AppendFormat(" and mat.fuseorgid = {0} and mat.fisui='1' {1} ", orgId, wlfzgl);
                //    builder.AppendFormat(" )a group by fmaterialid,Fhj,fmaterialnam");
                builder.AppendFormat(" )a group by fmaterialid,fmaterialnam,fispack,fpackqty )a group by fmaterialid,fmaterialnam,fispack");
                DBUtils.Execute(this.Context, builder.ToString());
                builder.Clear();


                builder.Append("/*oracle*/select a.Fmaterialid,");
                for (int i = 0; i < col.Count; i++)
                {
                    builder.AppendFormat(" F{0},", Convert.ToString(col[i]["fid"]));
                }
                builder.AppendFormat(" Fhj,");
                builder.AppendFormat(" FBs,");
                builder.AppendFormat(@"Fmaterialname ,b.fispack,c.fbaseqty from {0} a left join 
                                ( select st.fmaterialid, st.fbaseqty from T_STK_INVENTORY st 
                join t_bd_stock s on s.fstockid=st.FSTOCKID join t_bd_department d on d.fdeptsendstock = s.fstockid 
                 where  st.FSTOCKORGID = {1} and d.fdeptid= {2} ) c
                on a.fmaterialid=c.fmaterialid", tempTable01, orgId, deptId);
                builder.AppendFormat("  join t_bd_material  b  on b.fmaterialid=a.Fmaterialid");
                builder.AppendFormat("  order by b.FNO,a.Fmaterialid");
                DynamicObjectCollection col1 = DBUtils.ExecuteDynamicObject(this.Context, builder.ToString());
                builder.Clear();
                if (col1.Count != 0)
                {
                    _currInfo.GetDynamicObjectType(true);
                    jl = 1;
                    this.View.Model.CreateNewData();
                    this.View.Model.SetValue("FDEPT", deptId);
                    this.View.Model.SetValue("FDate", date);
                    this.View.Model.SetValue("FWLFL", wlfz);
                    this.View.Model.SetValue("FDJZT", ZTZ);
                    jl = 0;

                    for (int i = 1; i < col1.Count; i++)
                    {
                        this.View.Model.CreateNewEntryRow("FEntity");
                    }

                    for (int i = 0; i < col1.Count; i++)
                    {

                        this.View.Model.SetValue("FmatialId", Convert.ToString(col1[i]["Fmaterialid"]), i);
                        this.View.Model.SetValue("FmatialNam", Convert.ToString(col1[i]["Fmaterialname"]), i);
                        this.View.Model.SetValue("Fjskc", Convert.ToString(col1[i]["fbaseqty"]), i);
                        for (int j = 0; j < col.Count; j++)
                        {
                            this.View.Model.SetValue(string.Format("F{0}", Convert.ToString(col[j]["fid"])), Convert.ToDecimal(col1[i][string.Format("F{0}", Convert.ToString(col[j]["fid"]))]), i);
                            this.View.Model.SetValue(string.Format("F{0}To", Convert.ToString(col[j]["fid"])), Convert.ToDecimal(col1[i][string.Format("F{0}", Convert.ToString(col[j]["fid"]))]), i);
                        }
                        this.View.Model.SetValue("Fhj", Convert.ToDecimal(col1[i]["Fhj"]), i);
                        this.View.Model.SetValue("FBs", Convert.ToString(col1[i]["FBs"]), i);
                    }
                }

                builder.Append(" select null Fmaterialid,");
                for (int i = 0; i < col.Count; i++)
                {
                    builder.AppendFormat(" sum(F{0}) F{0},", Convert.ToString(col[i]["fid"]));
                }
                builder.Append(" sum(Fhj) Fhj,");
                builder.Append(@" null FBs,");
                builder.AppendFormat(" '合计' fmaterialname from {0}", tempTable01);
                DynamicObjectCollection col2 = DBUtils.ExecuteDynamicObject(this.Context, builder.ToString());
                builder.Clear();

                if (col1.Count != 0)
                {
                    this.View.Model.CreateNewEntryRow("FEntity");

                    this.View.Model.SetValue("FmatialId", Convert.ToString(col2[0]["Fmaterialid"]), col1.Count);
                    this.View.Model.SetValue("FmatialNam", Convert.ToString(col2[0]["Fmaterialname"]), col1.Count);

                    for (int j = 0; j < col.Count; j++)
                    {
                        this.View.Model.SetValue(string.Format("F{0}", Convert.ToString(col[j]["fid"])), Convert.ToDecimal(col2[0][string.Format("F{0}", Convert.ToString(col[j]["fid"]))]), col1.Count);
                        this.View.Model.SetValue(string.Format("F{0}To", Convert.ToString(col[j]["fid"])), Convert.ToDecimal(col2[0][string.Format("F{0}", Convert.ToString(col[j]["fid"]))]), col1.Count);

                    }
                    this.View.Model.SetValue("Fhj", Convert.ToDecimal(col2[0]["Fhj"]), col1.Count);
                    this.View.Model.SetValue("FBs", Convert.ToString(col2[0]["FBs"]), col1.Count);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                string delsql1 = string.Format("TRUNCATE TABLE {0}", tempTable01);
                DBUtils.Execute(this.Context, delsql1);
                string dropsql1 = string.Format("DROP TABLE {0}", tempTable01);
                DBUtils.Execute(this.Context, dropsql1);
            }
        }

        /// <summary> 给临时表赋表名
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static string AddTempTable(Context ctx)
        {
            return ServiceHelper.GetService<IDBService>().CreateTemporaryTableName(ctx);
        }

        /// <summary> 添加列
        /// 
        /// </summary>
        /// <param name="col"></param>
        private void getAddColumn(DynamicObjectCollection col)
        {
            Entity entity = _currInfo.GetEntity("FEntity");
            EntityAppearance entityApp = _currLayout.GetEntityAppearance("FEntity");
            // 清除全部字段
            int oldCount = entity.Fields.Count;
            for (int i = oldCount - 1; i >= 0; i--)
            {
                Field fld = entity.Fields[i];
                _currInfo.Remove(fld);
                Appearance fldApp = _currLayout.GetAppearance(fld.Key);
                _currLayout.Remove(fldApp);
            }


            TextField tfld1 = new TextField();
            tfld1.Key = "FmatialId";
            tfld1.Name = new LocaleValue("产品主键");
            tfld1.PropertyName = tfld1.Key;
            tfld1.EntityKey = "FEntity";
            tfld1.Entity = entity;
            _currInfo.Add(tfld1);
            TextFieldAppearance tfld1App = new TextFieldAppearance();
            tfld1App.Key = tfld1.Key;
            tfld1App.Caption = tfld1.Name;
            tfld1App.EntityKey = tfld1.EntityKey;
            tfld1App.Width = new LocaleValue("150");
            tfld1App.LabelWidth = new LocaleValue("80");
            tfld1App.Tabindex = 1;
            tfld1App.Field = tfld1;
            tfld1App.Visible = 28;
            _currLayout.Add(tfld1App);



            TextField tfld2 = new TextField();
            tfld2.Key = "FmatialNam";
            tfld2.Name = new LocaleValue("产品名称");
            tfld2.PropertyName = tfld2.Key;
            tfld2.EntityKey = "FEntity";
            tfld2.Entity = entity;
            _currInfo.Add(tfld2);
            TextFieldAppearance tfld2App = new TextFieldAppearance();
            tfld2App.Key = tfld2.Key;
            tfld2App.Caption = tfld2.Name;
            tfld2App.EntityKey = tfld2.EntityKey;
            tfld2App.Width = new LocaleValue("100");
            tfld2App.LabelWidth = new LocaleValue("80");
            tfld2App.Tabindex = 2;
            tfld2App.Locked = -1;
            tfld2App.Field = tfld2;
            _currLayout.Add(tfld2App);

            controls.Add(tfld2App);

            DecimalField afld0 = new DecimalField();
            afld0.Key = string.Format("Fjskc");
            afld0.Name = new LocaleValue(string.Format("即时库存数量"));
            afld0.PropertyName = afld0.Key;
            afld0.FieldPrecision = 23;
            afld0.FieldScale = 0;
            afld0.EntityKey = "FEntity";
            afld0.Entity = entity;
            _currInfo.Add(afld0);
            DecimalFieldAppearance afldApp0 = new DecimalFieldAppearance();
            afldApp0.Key = afld0.Key;
            afldApp0.Caption = afld0.Name;
            afldApp0.EntityKey = afld0.EntityKey;
            afldApp0.Width = new LocaleValue("100");
            afldApp0.LabelWidth = new LocaleValue("80");
            afldApp0.Tabindex = 3;
            afldApp0.Locked = -1;
            afldApp0.Field = afld0;
            _currLayout.Add(afldApp0);
            controls.Add(afldApp0);

            DecimalField afld = new DecimalField();
            afld.Key = string.Format("Fhj");
            afld.Name = new LocaleValue(string.Format("合计数量"));
            afld.PropertyName = afld.Key;
            afld.FieldPrecision = 23;
            afld.FieldScale = 0;
            afld.EntityKey = "FEntity";
            afld.Entity = entity;
            _currInfo.Add(afld);
            DecimalFieldAppearance afldApp = new DecimalFieldAppearance();
            afldApp.Key = afld.Key;
            afldApp.Caption = afld.Name;
            afldApp.EntityKey = afld.EntityKey;
            afldApp.Width = new LocaleValue("100");
            afldApp.LabelWidth = new LocaleValue("80");
            afldApp.Tabindex = 3;
            afldApp.Locked = -1;
            afldApp.Field = afld;
            _currLayout.Add(afldApp);
            controls.Add(afldApp);

            TextField afldbs = new TextField();
            afldbs.Key = string.Format("FBs");
            afldbs.Name = new LocaleValue(string.Format("件数"));
            afldbs.PropertyName = afldbs.Key;
            afldbs.EntityKey = "FEntity";
            afldbs.Entity = entity;
            _currInfo.Add(afldbs);
            TextFieldAppearance afldbsApp = new TextFieldAppearance();
            afldbsApp.Key = afldbs.Key;
            afldbsApp.Caption = afldbs.Name;
            afldbsApp.EntityKey = afldbs.EntityKey;
            afldbsApp.Width = new LocaleValue("100");
            afldbsApp.LabelWidth = new LocaleValue("80");
            afldbsApp.Tabindex = 4;
            afldbsApp.Locked = -1;
            afldbsApp.Field = afldbs;
            _currLayout.Add(afldbsApp);

            controls.Add(afldbsApp);

            for (int i = 0; i < col.Count; i++)
            {
                // 构建文本字段
                DecimalField afld1 = new DecimalField();
                afld1.Key = string.Format("F{0}", Convert.ToString(col[i]["fid"]));
                afld1.Name = new LocaleValue(string.Format("{0}", Convert.ToString(col[i]["fname"])));
                afld1.PropertyName = afld1.Key;
                afld1.FieldPrecision = 15;
                afld1.FieldScale = 0;
                afld1.EntityKey = "FEntity";
                afld1.Entity = entity;
                _currInfo.Add(afld1);
                DecimalFieldAppearance afld1App = new DecimalFieldAppearance();
                afld1App.Key = afld1.Key;
                afld1App.Caption = afld1.Name;
                afld1App.EntityKey = afld1.EntityKey;
                afld1App.Width = new LocaleValue("100");
                afld1App.LabelWidth = new LocaleValue("80");
                afld1App.Tabindex = 2 * i + 5;
                afld1App.Field = afld1;
                afld1App.Visible = 28;
                _currLayout.Add(afld1App);




                DecimalField afld2 = new DecimalField();
                afld2.Key = string.Format("F{0}To", Convert.ToString(col[i]["fid"]));
                afld2.Name = new LocaleValue(string.Format("{0}", Convert.ToString(col[i]["fname"])));
                afld2.PropertyName = afld2.Key;
                afld2.FieldPrecision = 15;//整体精度(包含小数精度＋整数精度)
                afld2.FieldScale = 0;//小数精度
                afld2.EntityKey = "FEntity";
                afld2.Entity = entity;
                _currInfo.Add(afld2);
                DecimalFieldAppearance afld2App = new DecimalFieldAppearance();
                afld2App.Key = afld2.Key;
                afld2App.Caption = afld2.Name;
                afld2App.EntityKey = afld2.EntityKey;
                afld2App.Width = new LocaleValue("100");
                afld2App.LabelWidth = new LocaleValue("80");
                afld2App.Tabindex = 2 * i + 6;
                afld2App.Field = afld2;
                _currLayout.Add(afld2App);
            }

        }

        public override void OnSetBusinessInfo(SetBusinessInfoArgs e)
        {
            // 复制界面元数据到本地变量
            FormMetadata currMeta = (FormMetadata)ObjectUtils.CreateCopy(
                this.View.OpenParameter.FormMetaData);
            _currInfo = currMeta.BusinessInfo;
            _currLayout = currMeta.GetLayoutInfo();
            // 用本地的元数据,替换动态表单引擎持有的元数据
            e.BusinessInfo = _currInfo;
            e.BillBusinessInfo = _currInfo;
        }

        public override void OnSetLayoutInfo(SetLayoutInfoArgs e)
        {
            e.LayoutInfo = _currLayout;
            e.BillLayoutInfo = _currLayout;
        }

        #endregion

    }
}

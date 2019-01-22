using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Rpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.TLMB.Contracts
{
    /// <summary>
    /// 服务契约
    /// </summary>
    [RpcServiceError]
    [ServiceContract]
    public interface ICommonService
    {
        /// <summary>
        /// 获取基础资料对象契约
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="formID">基础资料标识</param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObject GetBasicObject(Context ctx, string formID, long ObjectID);


        /// <summary>
        /// 创建单据
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="FormID"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        IOperationResult SaveBill(Context ctx, string FormID, DynamicObject dyObject);


        /// <summary>
        /// 提交单据
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="FormID"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        IOperationResult SubmitBill(Context ctx, string FormID, object[] ids);

        /// <summary>
        /// 审核单据
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="FormID"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        IOperationResult AuditBill(Context ctx, string FormID, object[] ids);

        /// <summary>
        /// 禁用单据
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="FormID">单据标识</param>
        /// <param name="ids">禁用单据内码集合</param>
        /// <param name="pkId">表主键列</param>
        /// <param name="tableName">表名</param>
        /// <param name="fieldName">禁用状态列</param>
        /// <param name="fieldValue">禁用值</param>
        /// <returns></returns>
        void SetState(Context ctx, string FormID, object[] ids, string pkId, string tableName, string fieldName, string fieldValue);

       

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="businessInfo"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        void Log(Context ctx, string Operation, IOperationResult returnResult);

        /// <summary>
        /// 组装费用申请单数据包
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="FormID"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        DynamicObject installCostRequestPackage(Context ctx, string FormID, Action<IDynamicFormViewService> fillBillPropertys, string BillTypeId);
    }
}

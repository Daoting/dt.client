﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2023-03-06 创建
******************************************************************************/
#endregion

#region 引用命名
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endregion

namespace Dt.Mgr.Rbac
{
    public partial class UserGroupList : Tab
    {
        #region 构造方法
        public UserGroupList()
        {
            InitializeComponent();
        }
        #endregion

        #region 公开
        public void Update(long p_releatedID)
        {
            _releatedID = p_releatedID;
            Menu["添加"].IsEnabled = _releatedID > 0;
            Refresh();
        }

        public async void Refresh()
        {
            if (_releatedID > 0)
            {
                _lv.Data = await GroupX.Query($"where exists ( select group_id from cm_user_group b where a.ID = b.group_id and user_id={_releatedID} )");
            }
            else
            {
                _lv.Data = null;
            }
        }
        #endregion

        #region 交互
        async void OnAdd(Mi e)
        {
            var dlg = new Group4UserDlg();
            if (await dlg.Show(_releatedID, e)
                && await RbacDs.AddUserGroups(_releatedID, dlg.SelectedIDs))
            {
                Refresh();
            }
        }

        async void OnDel(Mi e)
        {
            if (!await Kit.Confirm("确认要删除关联吗？"))
            {
                Kit.Msg("已取消删除！");
                return;
            }

            List<long> groupIDs;
            if (_lv.SelectionMode == Base.SelectionMode.Multiple)
            {
                groupIDs = (from row in _lv.SelectedRows
                            select row.ID).ToList();
            }
            else
            {
                groupIDs = new List<long> { e.Row.ID };
            }

            if (await RbacDs.RemoveUserGroups(_releatedID, groupIDs))
                Refresh();
        }
        #endregion

        #region 选择
        void OnSelectAll(Mi e)
        {
            _lv.SelectAll();
        }

        void OnMultiMode(Mi e)
        {
            _lv.SelectionMode = Base.SelectionMode.Multiple;
            Menu.HideExcept("删除", "全选", "取消");
        }

        void OnCancelMulti(Mi e)
        {
            _lv.SelectionMode = Base.SelectionMode.Single;
            Menu.ShowExcept("删除", "全选", "取消");
        }
        #endregion

        #region 内部
        UserWin _win => (UserWin)OwnWin;
        long _releatedID;
        #endregion
    }
}
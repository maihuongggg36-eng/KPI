/**
 * Hệ thống Quản lý KPI & Đánh giá Cán bộ Công chức
 * catalog-manager.js - Quản trị 18 Danh mục, Cây Phòng ban & Ngân hàng KPI
 */

(function () {
    'use strict';

    let currentActiveTab = 'kpi-items';
    let deleteTargetId = null;
    let deleteTargetType = null;

    // =========================================================================
    // 1. CHUYỂN ĐỔI TAB DANH MỤC
    // =========================================================================
    window.switchCatalogTab = function (tab) {
        currentActiveTab = tab;

        // Cập nhật tab buttons
        document.querySelectorAll('#catalogNavTabs .nav-link').forEach(btn => {
            if (btn.getAttribute('data-tab') === tab) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });

        // Cập nhật panels
        document.querySelectorAll('.catalog-tab-content').forEach(panel => {
            panel.classList.add('d-none');
        });
        const targetPanel = document.getElementById(`tab-${tab}`);
        if (targetPanel) {
            targetPanel.classList.remove('d-none');
        }

        // Reset bộ chọn nhiều
        resetBulkSelection();

        // Cập nhật URL
        window.history.replaceState(null, '', `/Catalog?tab=${tab}`);
    };

    // =========================================================================
    // 2. PHÍM TẮT HỆ THỐNG (KEYBOARD SHORTCUTS)
    // =========================================================================
    document.addEventListener('keydown', (e) => {
        // Ctrl + N: Thêm mới
        if (e.ctrlKey && e.key.toLowerCase() === 'n') {
            e.preventDefault();
            handleNewItemCurrentTab();
        }
        // Delete: Xóa các mục đã chọn
        else if (e.key === 'Delete' && !['input', 'textarea'].includes(document.activeElement.tagName.toLowerCase())) {
            const selected = getSelectedIds();
            if (selected.length > 0) {
                e.preventDefault();
                executeBulkAction('delete');
            }
        }
    });

    window.handleNewItemCurrentTab = function () {
        if (currentActiveTab === 'kpi-items') openNewKpiModal();
        else if (currentActiveTab === 'departments') openNewDeptModal(null);
        else if (currentActiveTab === 'positions') openNewPositionModal();
        else if (currentActiveTab === 'periods') openNewPeriodModal();
        else {
            if (window.toastManager) {
                window.toastManager.show({
                    title: 'Thêm Danh mục',
                    message: `Sử dụng nút thêm chuyên biệt trên bảng ${currentActiveTab}.`,
                    variant: 'info'
                });
            }
        }
    };

    // =========================================================================
    // 3. TÌM KIẾM REALTIME TRÊN BẢNG DANH MỤC
    // =========================================================================
    window.filterActiveCatalogTable = function () {
        const query = document.getElementById('catalogSearchInput')?.value.toLowerCase().trim() || '';
        const activeContainer = document.getElementById(`tab-${currentActiveTab}`);
        if (!activeContainer) return;

        const rows = activeContainer.querySelectorAll('tbody tr');
        rows.forEach(row => {
            const text = row.innerText.toLowerCase();
            row.style.display = text.includes(query) ? '' : 'none';
        });
    };

    // =========================================================================
    // 4. NGÂN HÀNG KPI: FILTER & VIEW SWITCHER
    // =========================================================================
    let currentPerspFilter = 'All';

    window.switchViewMode = function (mode) {
        const grid = document.getElementById('kpiGridContainer');
        const table = document.getElementById('kpiTableContainer');
        const gridBtn = document.getElementById('viewGridBtn');
        const tableBtn = document.getElementById('viewTableBtn');

        if (mode === 'grid') {
            grid?.classList.remove('d-none');
            table?.classList.add('d-none');
            gridBtn?.classList.add('btn-primary', 'active');
            gridBtn?.classList.remove('btn-light');
            tableBtn?.classList.remove('btn-primary', 'active');
            tableBtn?.classList.add('btn-light');
        } else {
            grid?.classList.add('d-none');
            table?.classList.remove('d-none');
            tableBtn?.classList.add('btn-primary', 'active');
            tableBtn?.classList.remove('btn-light');
            gridBtn?.classList.remove('btn-primary', 'active');
            gridBtn?.classList.add('btn-light');
        }
    };

    window.filterByPerspective = function (persp, btn) {
        currentPerspFilter = persp;
        document.querySelectorAll('.filter-persp-btn').forEach(b => {
            b.classList.remove('btn-primary');
            b.classList.add('btn-outline-secondary');
        });
        btn.classList.remove('btn-outline-secondary');
        btn.classList.add('btn-primary');
        filterKpiCards();
    };

    window.filterKpiCards = function () {
        const query = document.getElementById('kpiSearchInput')?.value.toLowerCase().trim() || '';
        const sector = document.getElementById('kpiSectorSelect')?.value || 'All';

        const cards = document.querySelectorAll('.kpi-card-item');
        const tableRows = document.querySelectorAll('.kpi-table-row');

        function matchFilter(el) {
            const code = el.getAttribute('data-code') || '';
            const name = el.getAttribute('data-name') || '';
            const desc = el.getAttribute('data-desc') || '';
            const persp = el.getAttribute('data-persp') || '';
            const cs = el.getAttribute('data-cs') || '';

            const matchQuery = !query || code.includes(query) || name.includes(query) || desc.includes(query);
            const matchPersp = currentPerspFilter === 'All' || persp === currentPerspFilter;
            const matchSector = sector === 'All' || cs === sector;

            return matchQuery && matchPersp && matchSector;
        }

        cards.forEach(card => card.style.display = matchFilter(card) ? '' : 'none');
        tableRows.forEach(row => row.style.display = matchFilter(row) ? '' : 'none');
    };

    // =========================================================================
    // 5. CRUD NGÂN HÀNG CHỈ TIÊU KPI
    // =========================================================================
    window.openNewKpiModal = function () {
        document.getElementById('kpiItemId').value = '0';
        document.getElementById('kpiItemModalLabel').innerHTML = '<i class="bi bi-journal-plus text-primary me-2"></i>Thêm Mới Chỉ Tiêu KPI Chuẩn';
        document.getElementById('kpiItemCode').value = '';
        document.getElementById('kpiItemName').value = '';
        document.getElementById('kpiItemDescription').value = '';
        document.getElementById('kpiItemFormula').value = 'HigherIsBetter';
        document.getElementById('kpiItemCivilServant').checked = false;

        const modalEl = document.getElementById('kpiItemModal');
        if (modalEl) new bootstrap.Modal(modalEl).show();
    };

    window.editKpiItem = function (id, code, name, catId, unitId, methodId, formula, isCs, desc) {
        document.getElementById('kpiItemId').value = id;
        document.getElementById('kpiItemModalLabel').innerHTML = '<i class="bi bi-pencil-square text-primary me-2"></i>Chỉnh Sửa Chỉ Tiêu KPI #' + code;
        document.getElementById('kpiItemCode').value = code;
        document.getElementById('kpiItemName').value = name;
        document.getElementById('kpiItemCategory').value = catId;
        document.getElementById('kpiItemUnit').value = unitId;
        document.getElementById('kpiItemMethod').value = methodId;
        document.getElementById('kpiItemFormula').value = formula;
        document.getElementById('kpiItemCivilServant').checked = isCs;
        document.getElementById('kpiItemDescription').value = desc || '';

        const modalEl = document.getElementById('kpiItemModal');
        if (modalEl) new bootstrap.Modal(modalEl).show();
    };

    window.submitKpiItemForm = function (e) {
        e.preventDefault();
        const id = parseInt(document.getElementById('kpiItemId').value) || 0;
        const payload = {
            id: id,
            code: document.getElementById('kpiItemCode').value.trim(),
            name: document.getElementById('kpiItemName').value.trim(),
            categoryId: parseInt(document.getElementById('kpiItemCategory').value),
            unitId: parseInt(document.getElementById('kpiItemUnit').value),
            methodId: parseInt(document.getElementById('kpiItemMethod').value),
            scoringFormula: document.getElementById('kpiItemFormula').value,
            isCivilServantOnly: document.getElementById('kpiItemCivilServant').checked,
            description: document.getElementById('kpiItemDescription').value.trim(),
            isActive: true
        };

        const isCreate = id === 0;
        const url = isCreate ? '/Catalog/CreateKpiItem' : '/Catalog/UpdateKpiItem';
        const method = isCreate ? 'POST' : 'PUT';

        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })
        .then(res => res.json())
        .then(res => {
            if (res.success) {
                bootstrap.Modal.getInstance(document.getElementById('kpiItemModal'))?.hide();
                if (window.toastManager) {
                    window.toastManager.show({ title: 'Thành công', message: res.message, variant: 'success' });
                }
                setTimeout(() => window.location.reload(), 800);
            } else {
                alert(res.message);
            }
        });
    };

    window.confirmDeleteKpi = function (id, name) {
        deleteTargetId = id;
        deleteTargetType = 'kpi';
        document.getElementById('deleteConfirmMessage').innerHTML = `Bạn có chắc chắn muốn xóa chỉ tiêu <strong>${name}</strong> khỏi ngân hàng KPI?`;
        new bootstrap.Modal(document.getElementById('deleteConfirmModal')).show();
    };

    window.openCloneModal = function (id, code, name) {
        document.getElementById('cloneSourceId').value = id;
        document.getElementById('cloneSourceName').textContent = `${code} - ${name}`;
        document.getElementById('cloneNewCode').value = `${code}-V2`;
        document.getElementById('cloneNewName').value = `${name} (Bản sao)`;
        new bootstrap.Modal(document.getElementById('cloneKpiModal')).show();
    };

    window.submitCloneKpi = function (e) {
        e.preventDefault();
        const payload = {
            sourceId: parseInt(document.getElementById('cloneSourceId').value),
            newCode: document.getElementById('cloneNewCode').value.trim(),
            newName: document.getElementById('cloneNewName').value.trim()
        };

        fetch('/Catalog/CloneKpiItem', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })
        .then(res => res.json())
        .then(res => {
            if (res.success) {
                bootstrap.Modal.getInstance(document.getElementById('cloneKpiModal'))?.hide();
                if (window.toastManager) {
                    window.toastManager.show({ title: 'Nhân bản Thành công', message: res.message, variant: 'success' });
                }
                setTimeout(() => window.location.reload(), 800);
            } else {
                alert(res.message);
            }
        });
    };

    // =========================================================================
    // 6. CÂY PHÂN CẤP PHÒNG BAN (TREE HIERARCHY)
    // =========================================================================
    window.toggleTreeNode = function (btn) {
        const parentNode = btn.closest('.dept-tree-node');
        const children = parentNode?.querySelector('.dept-children-container');
        if (children) {
            const isHidden = children.classList.toggle('d-none');
            const icon = btn.querySelector('i');
            if (icon) {
                icon.className = isHidden ? 'bi bi-chevron-right' : 'bi bi-chevron-down';
            }
        }
    };

    window.expandAllTreeNodes = function () {
        document.querySelectorAll('.dept-children-container').forEach(c => c.classList.remove('d-none'));
        document.querySelectorAll('.node-toggle-btn i').forEach(icon => {
            if (icon.classList.contains('bi-chevron-right')) icon.className = 'bi bi-chevron-down';
        });
    };

    window.collapseAllTreeNodes = function () {
        document.querySelectorAll('.dept-children-container').forEach(c => c.classList.add('d-none'));
        document.querySelectorAll('.node-toggle-btn i').forEach(icon => {
            if (icon.classList.contains('bi-chevron-down')) icon.className = 'bi bi-chevron-right';
        });
    };

    window.openNewDeptModal = function (parentId) {
        document.getElementById('deptId').value = '0';
        document.getElementById('deptCode').value = '';
        document.getElementById('deptName').value = '';
        const select = document.getElementById('deptParentId');
        if (select && parentId) select.value = parentId;
        new bootstrap.Modal(document.getElementById('deptModal')).show();
    };

    window.editDeptNode = function (id, code, name) {
        document.getElementById('deptId').value = id;
        document.getElementById('deptCode').value = code;
        document.getElementById('deptName').value = name;
        new bootstrap.Modal(document.getElementById('deptModal')).show();
    };

    window.submitDeptForm = function (e) {
        e.preventDefault();
        const id = parseInt(document.getElementById('deptId').value) || 0;
        const parentVal = document.getElementById('deptParentId')?.value;
        const payload = {
            id: id,
            parentId: parentVal ? parseInt(parentVal) : null,
            code: document.getElementById('deptCode').value.trim(),
            name: document.getElementById('deptName').value.trim(),
            status: 'Active'
        };

        const isCreate = id === 0;
        const url = isCreate ? '/Catalog/CreateDepartment' : '/Catalog/UpdateDepartment';
        const method = isCreate ? 'POST' : 'PUT';

        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })
        .then(res => res.json())
        .then(res => {
            if (res.success) {
                bootstrap.Modal.getInstance(document.getElementById('deptModal'))?.hide();
                if (window.toastManager) {
                    window.toastManager.show({ title: 'Thành công', message: res.message, variant: 'success' });
                }
                setTimeout(() => window.location.reload(), 800);
            } else {
                alert(res.message);
            }
        });
    };

    window.openMoveDeptModal = function (id, name) {
        document.getElementById('moveDeptId').value = id;
        document.getElementById('moveDeptName').textContent = name;
        new bootstrap.Modal(document.getElementById('moveDeptModal')).show();
    };

    window.submitMoveDept = function (e) {
        e.preventDefault();
        const id = parseInt(document.getElementById('moveDeptId').value);
        const parentVal = document.getElementById('moveNewParentSelect')?.value;

        fetch('/Catalog/MoveDepartment', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                departmentId: id,
                newParentId: parentVal ? parseInt(parentVal) : null
            })
        })
        .then(res => res.json())
        .then(res => {
            if (res.success) {
                bootstrap.Modal.getInstance(document.getElementById('moveDeptModal'))?.hide();
                if (window.toastManager) {
                    window.toastManager.show({ title: 'Đã di chuyển', message: res.message, variant: 'success' });
                }
                setTimeout(() => window.location.reload(), 800);
            } else {
                alert(res.message);
            }
        });
    };

    window.confirmDeleteDept = function (id, name) {
        deleteTargetId = id;
        deleteTargetType = 'department';
        document.getElementById('deleteConfirmMessage').innerHTML = `Bạn có chắc chắn muốn xóa phòng ban <strong>${name}</strong>?`;
        new bootstrap.Modal(document.getElementById('deleteConfirmModal')).show();
    };

    // =========================================================================
    // 7. XÁC NHẬN XÓA CHUNG
    // =========================================================================
    document.getElementById('confirmDeleteBtn')?.addEventListener('click', () => {
        if (!deleteTargetId) return;

        let url = '';
        if (deleteTargetType === 'kpi') url = `/Catalog/DeleteKpiItem?id=${deleteTargetId}`;
        else if (deleteTargetType === 'department') url = `/Catalog/DeleteDepartment?id=${deleteTargetId}`;
        else if (deleteTargetType === 'position') url = `/Catalog/DeletePosition?id=${deleteTargetId}`;

        fetch(url, { method: 'DELETE' })
            .then(res => res.json())
            .then(res => {
                bootstrap.Modal.getInstance(document.getElementById('deleteConfirmModal'))?.hide();
                if (res.success) {
                    if (window.toastManager) {
                        window.toastManager.show({ title: 'Đã xóa', message: res.message, variant: 'success' });
                    }
                    setTimeout(() => window.location.reload(), 800);
                } else {
                    alert(res.message);
                }
            });
    });

    // =========================================================================
    // 8. CHỨC DANH, KỲ ĐÁNH GIÁ & CẤU HÌNH THAM SỐ
    // =========================================================================
    window.openNewPositionModal = function () {
        new bootstrap.Modal(document.getElementById('positionModal')).show();
    };

    window.submitPositionForm = function (e) {
        e.preventDefault();
        const payload = {
            code: document.getElementById('posCode').value.trim(),
            name: document.getElementById('posName').value.trim(),
            rankLevel: document.getElementById('posRankLevel').value,
            isLeadership: document.getElementById('posIsLeadership').checked
        };

        fetch('/Catalog/CreatePosition', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })
        .then(res => res.json())
        .then(res => {
            if (res.success) {
                bootstrap.Modal.getInstance(document.getElementById('positionModal'))?.hide();
                if (window.toastManager) window.toastManager.show({ title: 'Thành công', message: res.message, variant: 'success' });
                setTimeout(() => window.location.reload(), 800);
            }
        });
    };

    window.deletePosition = function (id, name) {
        deleteTargetId = id;
        deleteTargetType = 'position';
        document.getElementById('deleteConfirmMessage').innerHTML = `Xác nhận xóa chức danh <strong>${name}</strong>?`;
        new bootstrap.Modal(document.getElementById('deleteConfirmModal')).show();
    };

    window.changePeriodLifecycle = function (periodId, status) {
        fetch('/Catalog/ChangePeriodStatus', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ periodId: periodId, newStatus: status })
        })
        .then(res => res.json())
        .then(res => {
            if (window.toastManager) window.toastManager.show({ title: 'Lifecycle', message: res.message, variant: res.success ? 'success' : 'danger' });
        });
    };

    window.saveSystemConfig = function (key) {
        const val = document.getElementById(`config-val-${key}`)?.value || '';
        fetch('/Catalog/UpdateConfig', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ configKey: key, configValue: val })
        })
        .then(res => res.json())
        .then(res => {
            if (window.toastManager) window.toastManager.show({ title: 'Cấu hình', message: res.message, variant: res.success ? 'success' : 'danger' });
        });
    };

    // =========================================================================
    // 9. BULK ACTIONS & CHỌN NHIỀU
    // =========================================================================
    window.toggleSelectAll = function (chk) {
        const activeContainer = document.getElementById(`tab-${currentActiveTab}`);
        activeContainer?.querySelectorAll('.row-chk').forEach(c => c.checked = chk.checked);
        updateSelectedCount();
    };

    window.updateSelectedCount = function () {
        const selected = getSelectedIds();
        const countEl = document.getElementById('selectedCount');
        const btn = document.getElementById('bulkActionBtn');
        if (countEl) countEl.textContent = selected.length;
        if (btn) btn.disabled = selected.length === 0;
    };

    function getSelectedIds() {
        const activeContainer = document.getElementById(`tab-${currentActiveTab}`);
        const checked = activeContainer?.querySelectorAll('.row-chk:checked') || [];
        return Array.from(checked).map(c => parseInt(c.value));
    }

    function resetBulkSelection() {
        document.querySelectorAll('.row-chk').forEach(c => c.checked = false);
        document.querySelectorAll('.select-all-chk').forEach(c => c.checked = false);
        updateSelectedCount();
    }

    window.executeBulkAction = function (action) {
        const ids = getSelectedIds();
        if (ids.length === 0) return;

        if (action === 'delete' && !confirm(`Bạn có chắc chắn muốn xóa ${ids.length} mục đã chọn không?`)) {
            return;
        }

        fetch('/Catalog/BulkAction', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                catalogType: currentActiveTab,
                actionType: action,
                ids: ids
            })
        })
        .then(res => res.json())
        .then(res => {
            if (res.success) {
                if (window.toastManager) window.toastManager.show({ title: 'Thao tác Hàng loạt', message: res.message, variant: 'success' });
                setTimeout(() => window.location.reload(), 800);
            } else {
                alert(res.message);
            }
        });
    };

    window.exportCurrentCatalogCsv = function () {
        window.location.href = `/Catalog/ExportCsv?type=${currentActiveTab}`;
    };

    // Khởi chạy khi tải trang
    document.addEventListener('DOMContentLoaded', () => {
        const urlParams = new URLSearchParams(window.location.search);
        const tabParam = urlParams.get('tab');
        if (tabParam) switchCatalogTab(tabParam);
    });

})();

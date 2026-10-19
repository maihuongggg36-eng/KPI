<?php
namespace App\Controllers;

use App\Models\KpiCatalog;
use App\Models\Database;
use App\Services\AuditService;

class CatalogController {
    public function index(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $masterItems = KpiCatalog::getMasterItems();
        $categories = KpiCatalog::getCategories();
        $units = KpiCatalog::getUnits();
        $activePeriod = KpiCatalog::getActivePeriod();

        $db = Database::getConnection();
        $periods = $db->query("SELECT * FROM evaluation_periods ORDER BY year DESC, id DESC")->fetchAll();
        $departments = $db->query("SELECT * FROM departments ORDER BY level ASC, id ASC")->fetchAll();
        $positions = $db->query("SELECT * FROM positions ORDER BY is_leadership DESC, id ASC")->fetchAll();

        require BASE_PATH . '/app/Views/catalogs/index.php';
    }

    public function storeKpiItem(): void {
        if (empty($_SESSION['user_id']) || !in_array($_SESSION['role'], ['HR Admin', 'Super Admin', 'Director'])) {
            $_SESSION['error'] = 'Bạn không có quyền thêm mới chỉ tiêu KPI trong danh mục.';
            header('Location: /catalogs');
            exit;
        }

        $code = trim($_POST['code'] ?? '');
        $name = trim($_POST['name'] ?? '');
        $categoryId = intval($_POST['category_id'] ?? 1);
        $unitId = intval($_POST['unit_id'] ?? 1);
        $description = trim($_POST['description'] ?? '');
        $formula = $_POST['scoring_formula'] ?? 'HigherIsBetter';
        $isCivilServant = !empty($_POST['is_civil_servant_only']) ? 1 : 0;

        if (empty($code) || empty($name)) {
            $_SESSION['error'] = 'Mã chỉ tiêu và Tên chỉ tiêu không được để trống.';
            header('Location: /catalogs');
            exit;
        }

        try {
            $db = Database::getConnection();
            $stmt = $db->prepare("
                INSERT INTO kpi_master_items (category_id, unit_id, code, name, description, scoring_formula, is_civil_servant_only)
                VALUES (:cat, :unit, :code, :name, :desc, :formula, :cs)
            ");
            $stmt->execute([
                'cat' => $categoryId,
                'unit' => $unitId,
                'code' => $code,
                'name' => $name,
                'desc' => $description,
                'formula' => $formula,
                'cs' => $isCivilServant
            ]);

            $newId = $db->lastInsertId();
            AuditService::log($_SESSION['user_id'], 'CREATE_MASTER_KPI', 'kpi_master_items', $newId, "Tạo chỉ tiêu danh mục: $code - $name");

            $_SESSION['flash_message'] = "Đã bổ sung thành công chỉ tiêu '<strong>$name</strong>' vào ngân hàng KPI.";
        } catch (\Exception $e) {
            $_SESSION['error'] = 'Không thể lưu chỉ tiêu (có thể mã chỉ tiêu đã tồn tại): ' . $e->getMessage();
        }

        header('Location: /catalogs');
        exit;
    }
}

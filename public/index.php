<?php
/**
 * Entry Point & Router for KPI Evaluation Management System
 */

require_once dirname(__DIR__) . '/config/app.php';

// PSR-4 Autoloader
spl_autoload_register(function ($class) {
    $prefix = 'App\\';
    $baseDir = BASE_PATH . '/app/';

    $len = strlen($prefix);
    if (strncmp($prefix, $class, $len) !== 0) {
        return;
    }

    $relativeClass = substr($class, $len);
    $file = $baseDir . str_replace('\\', '/', $relativeClass) . '.php';

    if (file_exists($file)) {
        require_once $file;
    }
});

// Phân tích URI
$uri = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
$method = $_SERVER['REQUEST_METHOD'];

// Loại bỏ trailing slash nếu có (trừ root '/')
if ($uri !== '/' && str_ends_with($uri, '/')) {
    $uri = rtrim($uri, '/');
}

// Router Dispatcher
try {
    switch ($uri) {
        case '/':
            header('Location: /dashboard');
            exit;

        case '/login':
            $controller = new \App\Controllers\AuthController();
            if ($method === 'POST') {
                $controller->login();
            } else {
                $controller->showLogin();
            }
            break;

        case '/logout':
            (new \App\Controllers\AuthController())->logout();
            break;

        case '/dashboard':
            (new \App\Controllers\DashboardController())->index();
            break;

        case '/kpi/my':
            (new \App\Controllers\KpiPlanningController())->myKpi();
            break;

        case '/kpi/assignments':
            (new \App\Controllers\KpiPlanningController())->listAssignments();
            break;

        case '/kpi/assign':
            $controller = new \App\Controllers\KpiPlanningController();
            if ($method === 'POST') {
                $controller->storeAssign();
            } else {
                $controller->showAssignForm();
            }
            break;

        case '/evaluation/self-assess':
            $controller = new \App\Controllers\EvaluationController();
            if ($method === 'POST') {
                $controller->submitSelfAssess();
            } else {
                $controller->showSelfAssess();
            }
            break;

        case '/evaluation/manager-review':
            $controller = new \App\Controllers\EvaluationController();
            if ($method === 'POST') {
                $controller->submitManagerReview();
            } else {
                $controller->showManagerReview();
            }
            break;

        case '/evaluation/approve':
            if ($method === 'POST') {
                (new \App\Controllers\EvaluationController())->approve();
            } else {
                header('Location: /dashboard');
            }
            break;

        case '/civil-servant':
            (new \App\Controllers\CivilServantController())->index();
            break;

        case '/civil-servant/form':
            (new \App\Controllers\CivilServantController())->showForm();
            break;

        case '/civil-servant/update':
            if ($method === 'POST') {
                (new \App\Controllers\CivilServantController())->updateClassification();
            } else {
                header('Location: /civil-servant');
            }
            break;

        case '/catalogs':
            (new \App\Controllers\CatalogController())->index();
            break;

        case '/catalogs/kpi/store':
            if ($method === 'POST') {
                (new \App\Controllers\CatalogController())->storeKpiItem();
            } else {
                header('Location: /catalogs');
            }
            break;

        case '/reports':
            (new \App\Controllers\ReportController())->index();
            break;

        case '/reports/export-csv':
            (new \App\Controllers\ReportController())->exportCsv();
            break;

        case '/kpi/view':
            (new \App\Controllers\KpiPlanningController())->viewKpi();
            break;

        case '/payroll':
            (new \App\Controllers\PayrollController())->index();
            break;

        case '/payroll/calculate':
            if ($method === 'POST') {
                (new \App\Controllers\PayrollController())->calculate();
            } else {
                header('Location: /payroll');
            }
            break;

        case '/payroll/update':
            if ($method === 'POST') {
                (new \App\Controllers\PayrollController())->update();
            } else {
                header('Location: /payroll');
            }
            break;

        case '/payroll/pay-cash':
            (new \App\Controllers\PayrollController())->payCash();
            break;

        case '/payroll/pay-all-cash':
            if ($method === 'POST') {
                (new \App\Controllers\PayrollController())->payAllCash();
            } else {
                header('Location: /payroll');
            }
            break;

        case '/payroll/voucher':
            (new \App\Controllers\PayrollController())->voucher();
            break;

        case '/payroll/cash-sheet':
            (new \App\Controllers\PayrollController())->cashSheet();
            break;

        case '/payroll/my-payslip':
            (new \App\Controllers\PayrollController())->myPayslip();
            break;

        default:
            http_response_code(404);
            echo "<!DOCTYPE html><html lang='vi'><head><meta charset='UTF-8'><title>404 Not Found</title><link rel='stylesheet' href='/css/style.css'></head><body><div style='text-align: center; padding: 100px 20px; font-family: sans-serif;'><h1 style='font-size: 72px; color: #ef4444;'>404</h1><h2>Trang không tồn tại</h2><p style='color: #64748b; margin: 16px 0 24px;'>Đường dẫn bạn yêu cầu không hợp lệ hoặc đã bị thay đổi.</p><a href='/dashboard' class='btn btn-primary'>Về Bàn làm việc</a></div></body></html>";
            break;
    }
} catch (\Throwable $e) {
    http_response_code(500);
    echo "<h1>Lỗi Hệ thống:</h1><p>" . htmlspecialchars($e->getMessage()) . "</p><pre>" . htmlspecialchars($e->getTraceAsString()) . "</pre>";
}

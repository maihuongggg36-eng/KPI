<?php
namespace App\Controllers;

use App\Models\Evaluation;
use App\Models\KpiCatalog;
use App\Models\KpiAssignment;
use App\Models\User;

class DashboardController {
    public function index(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $role = $_SESSION['role'] ?? 'Employee';
        $period = KpiCatalog::getActivePeriod() ?? ['id' => 1, 'name' => 'Kỳ đánh giá Quý 1/2026', 'year' => 2026];
        $stats = Evaluation::getDashboardStats($period['id']);

        if (in_array($role, ['Director', 'Super Admin'])) {
            $recentAssignments = KpiAssignment::getAllByPeriod($period['id']);
            require BASE_PATH . '/app/Views/dashboard/director.php';
        } elseif ($role === 'Manager') {
            $deptId = $_SESSION['department_id'] ?? 3;
            $deptStaff = User::getEmployeesByDepartment($deptId);
            $recentAssignments = KpiAssignment::getAllByPeriod($period['id']);
            require BASE_PATH . '/app/Views/dashboard/manager.php';
        } elseif ($role === 'Accountant') {
            $recentAssignments = KpiAssignment::getAllByPeriod($period['id']);
            $payrollStats = \App\Models\Payroll::getStats($period['id']);
            $recentPayrolls = \App\Models\Payroll::getAllByPeriod($period['id']);
            require BASE_PATH . '/app/Views/dashboard/accountant.php';
        } else {
            // Employee
            $userId = $_SESSION['user_id'];
            $myAssignment = KpiAssignment::getByPeriodAndEmployee($period['id'], $userId);
            $myEvaluation = $myAssignment ? Evaluation::getByAssignmentId($myAssignment['id']) : null;
            require BASE_PATH . '/app/Views/dashboard/employee.php';
        }
    }
}

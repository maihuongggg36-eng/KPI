<?php
namespace App\Controllers;

use App\Models\User;
use App\Services\AuditService;

class AuthController {
    public function showLogin(): void {
        if (!empty($_GET['quick_switch'])) {
            $username = trim($_GET['quick_switch']);
            $user = User::findByUsername($username);
            if ($user) {
                $_SESSION['user_id'] = $user['id'];
                $_SESSION['username'] = $user['username'];
                $_SESSION['full_name'] = $user['full_name'];
                $_SESSION['role'] = $user['role'];
                $_SESSION['department_id'] = $user['department_id'];
                $_SESSION['department_name'] = $user['department_name'];
                $_SESSION['position_name'] = $user['position_name'];
                $_SESSION['is_civil_servant'] = $user['is_civil_servant'];
                $_SESSION['flash_message'] = "Đã chuyển phiên đăng nhập sang: <strong>{$user['full_name']}</strong> ({$user['role']})";
                header('Location: /dashboard');
                exit;
            }
        }

        if (!empty($_SESSION['user_id'])) {
            header('Location: /dashboard');
            exit;
        }
        $error = $_SESSION['error'] ?? null;
        unset($_SESSION['error']);
        require BASE_PATH . '/app/Views/auth/login.php';
    }

    public function login(): void {
        $username = trim($_POST['username'] ?? '');
        $password = trim($_POST['password'] ?? '');

        $user = User::findByUsername($username);

        if ($user && password_verify($password, $user['password'])) {
            $_SESSION['user_id'] = $user['id'];
            $_SESSION['username'] = $user['username'];
            $_SESSION['full_name'] = $user['full_name'];
            $_SESSION['role'] = $user['role'];
            $_SESSION['department_id'] = $user['department_id'];
            $_SESSION['department_name'] = $user['department_name'];
            $_SESSION['position_name'] = $user['position_name'];
            $_SESSION['is_civil_servant'] = $user['is_civil_servant'];

            AuditService::log($user['id'], 'LOGIN_SUCCESS', 'users', $user['id'], 'Đăng nhập thành công');

            header('Location: /dashboard');
            exit;
        } else {
            $_SESSION['error'] = 'Tên đăng nhập hoặc mật khẩu không chính xác!';
            header('Location: /login');
            exit;
        }
    }

    public function switchRole(): void {
        // Hỗ trợ chuyển đổi nhanh tài khoản mẫu để test các vai trò
        $userId = intval($_GET['user_id'] ?? 1);
        $user = User::findById($userId);

        if ($user) {
            $_SESSION['user_id'] = $user['id'];
            $_SESSION['username'] = $user['username'];
            $_SESSION['full_name'] = $user['full_name'];
            $_SESSION['role'] = $user['role'];
            $_SESSION['department_id'] = $user['department_id'];
            $_SESSION['department_name'] = $user['department_name'];
            $_SESSION['position_name'] = $user['position_name'];
            $_SESSION['is_civil_servant'] = $user['is_civil_servant'];

            $_SESSION['flash_message'] = "Đã chuyển sang vai trò: <strong>{$user['role']}</strong> ({$user['full_name']})";
        }
        header('Location: /dashboard');
        exit;
    }

    public function logout(): void {
        if (!empty($_SESSION['user_id'])) {
            AuditService::log($_SESSION['user_id'], 'LOGOUT', 'users', $_SESSION['user_id'], 'Đăng xuất khỏi hệ thống');
        }
        session_destroy();
        header('Location: /login');
        exit;
    }
}

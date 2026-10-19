<?php
namespace App\Models;

use PDO;

class User {
    public static function findById(int $id): ?array {
        $db = Database::getConnection();
        $stmt = $db->prepare("
            SELECT u.*, d.name as department_name, p.name as position_name 
            FROM users u
            LEFT JOIN departments d ON u.department_id = d.id
            LEFT JOIN positions p ON u.position_id = p.id
            WHERE u.id = ?
        ");
        $stmt->execute([$id]);
        $user = $stmt->fetch();
        return $user ?: null;
    }

    public static function findByUsername(string $username): ?array {
        $db = Database::getConnection();
        $stmt = $db->prepare("
            SELECT u.*, d.name as department_name, p.name as position_name 
            FROM users u
            LEFT JOIN departments d ON u.department_id = d.id
            LEFT JOIN positions p ON u.position_id = p.id
            WHERE u.username = ? OR u.email = ?
        ");
        $stmt->execute([$username, $username]);
        $user = $stmt->fetch();
        return $user ?: null;
    }

    public static function getAll(): array {
        $db = Database::getConnection();
        $stmt = $db->query("
            SELECT u.*, d.name as department_name, p.name as position_name 
            FROM users u
            LEFT JOIN departments d ON u.department_id = d.id
            LEFT JOIN positions p ON u.position_id = p.id
            ORDER BY u.id ASC
        ");
        return $stmt->fetchAll();
    }

    public static function getEmployeesByDepartment(int $deptId): array {
        $db = Database::getConnection();
        $stmt = $db->prepare("
            SELECT u.*, p.name as position_name 
            FROM users u
            LEFT JOIN positions p ON u.position_id = p.id
            WHERE u.department_id = ?
            ORDER BY u.full_name ASC
        ");
        $stmt->execute([$deptId]);
        return $stmt->fetchAll();
    }
}

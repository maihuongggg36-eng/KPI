<?php
namespace App\Services;

use App\Models\Database;
use PDO;

class AuditService {
    public static function log(?int $userId, string $action, string $entityName, ?int $entityId, string $details = ''): void {
        try {
            $db = Database::getConnection();
            $ip = $_SERVER['REMOTE_ADDR'] ?? '127.0.0.1';
            $stmt = $db->prepare("
                INSERT INTO audit_logs (user_id, action, entity_name, entity_id, details, ip_address) 
                VALUES (?, ?, ?, ?, ?, ?)
            ");
            $stmt->execute([$userId, $action, $entityName, $entityId, $details, $ip]);
        } catch (\Exception $e) {
            // Log silently
            error_log("Audit log error: " . $e->getMessage());
        }
    }
}

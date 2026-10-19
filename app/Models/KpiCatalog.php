<?php
namespace App\Models;

use PDO;

class KpiCatalog {
    public static function getCategories(): array {
        $db = Database::getConnection();
        return $db->query("SELECT * FROM kpi_categories ORDER BY id ASC")->fetchAll();
    }

    public static function getUnits(): array {
        $db = Database::getConnection();
        return $db->query("SELECT * FROM measurement_units ORDER BY id ASC")->fetchAll();
    }

    public static function getPeriods(): array {
        $db = Database::getConnection();
        return $db->query("SELECT * FROM evaluation_periods ORDER BY year DESC, id DESC")->fetchAll();
    }

    public static function getActivePeriod(): ?array {
        $db = Database::getConnection();
        $stmt = $db->query("SELECT * FROM evaluation_periods WHERE status = 'Open' LIMIT 1");
        $period = $stmt->fetch();
        return $period ?: null;
    }

    public static function getMasterItems(?int $categoryId = null): array {
        $db = Database::getConnection();
        if ($categoryId) {
            $stmt = $db->prepare("
                SELECT k.*, c.name as category_name, u.name as unit_name, u.symbol, u.symbol as unit_symbol
                FROM kpi_master_items k
                JOIN kpi_categories c ON k.category_id = c.id
                JOIN measurement_units u ON k.unit_id = u.id
                WHERE k.category_id = ? AND k.is_active = 1
                ORDER BY k.id ASC
            ");
            $stmt->execute([$categoryId]);
            return $stmt->fetchAll();
        } else {
            return $db->query("
                SELECT k.*, c.name as category_name, u.name as unit_name, u.symbol, u.symbol as unit_symbol
                FROM kpi_master_items k
                JOIN kpi_categories c ON k.category_id = c.id
                JOIN measurement_units u ON k.unit_id = u.id
                WHERE k.is_active = 1
                ORDER BY k.id ASC
            ")->fetchAll();
        }
    }

    public static function createItem(array $data): int {
        $db = Database::getConnection();
        $stmt = $db->prepare("
            INSERT INTO kpi_master_items (category_id, unit_id, code, name, description, scoring_formula, is_civil_servant_only)
            VALUES (?, ?, ?, ?, ?, ?, ?)
        ");
        $stmt->execute([
            $data['category_id'],
            $data['unit_id'],
            $data['code'],
            $data['name'],
            $data['description'] ?? '',
            $data['scoring_formula'] ?? 'HigherIsBetter',
            $data['is_civil_servant_only'] ?? 0
        ]);
        return (int)$db->lastInsertId();
    }
}

<?php
/**
 * Database Configuration
 * Hỗ trợ SQLite (mặc định sẵn sàng 100% không cần cài đặt thêm) và MySQL
 */

return [
    'driver' => 'sqlite', // 'sqlite' hoặc 'mysql'
    
    'sqlite' => [
        'database' => dirname(__DIR__) . '/database/kpi_system.db',
    ],

    'mysql' => [
        'host' => '127.0.0.1',
        'port' => 3306,
        'database' => 'kpi_system',
        'username' => 'root',
        'password' => '',
        'charset' => 'utf8mb4',
    ]
];

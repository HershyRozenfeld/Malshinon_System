
-- הגדרת קידוד לתמיכה מלאה בעברית ותווים מיוחדים
SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;

-- יצירת טבלת האנשים אם לא קיימת
CREATE TABLE IF NOT EXISTS People (
    id INT AUTO_INCREMENT PRIMARY KEY,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    secret_code VARCHAR(100) NOT NULL UNIQUE,
    type ENUM('reporter', 'target', 'both', 'potential_agent') DEFAULT 'reporter',
    num_reports INT DEFAULT 0,
    num_mentions INT DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- יצירת טבלת הדיווחים אם לא קיימת
CREATE TABLE IF NOT EXISTS IntelReports (
    id INT AUTO_INCREMENT PRIMARY KEY,
    reporter_id INT NOT NULL,
    target_id INT NOT NULL,
    text TEXT NOT NULL,
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (reporter_id) REFERENCES People(id) ON DELETE CASCADE,
    FOREIGN KEY (target_id) REFERENCES People(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- יצירת טבלת התראות אם לא קיימת
CREATE TABLE IF NOT EXISTS Alerts (
    id INT AUTO_INCREMENT PRIMARY KEY,
    target_id INT NOT NULL,
    reason TEXT NOT NULL,
    start_time DATETIME NOT NULL,
    end_time DATETIME NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (target_id) REFERENCES People(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

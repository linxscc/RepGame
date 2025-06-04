-- CardDeck数据库表结构和数据插入SQL语句
-- 创建数据库（可选）
-- CREATE DATABASE IF NOT EXISTS RepGame CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
-- USE RepGame;

-- 创建卡牌表
CREATE TABLE IF NOT EXISTS card_deck (
    id INT AUTO_INCREMENT PRIMARY KEY COMMENT '卡牌ID，自增主键',
    name VARCHAR(50) NOT NULL COMMENT '卡牌名称',
    cards_num INT NOT NULL DEFAULT 0 COMMENT '卡牌数量',
    damage DECIMAL(4,1) NOT NULL DEFAULT 0.0 COMMENT '卡牌伤害值',
    targetname VARCHAR(50) NULL COMMENT '升级目标卡牌名称',
    level TINYINT NOT NULL DEFAULT 1 COMMENT '卡牌等级',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    INDEX idx_name (name),
    INDEX idx_level (level),
    INDEX idx_targetname (targetname)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='卡牌数据表';

-- 插入卡牌数据
INSERT INTO card_deck (name, cards_num, damage, targetname, level) VALUES
-- 木匠系列
('木匠学徒', 17, 1.0, '木匠', 1),
('木匠', 5, 3.0, '百工专家', 2),
('百工专家', 1, 9.0, NULL, 3),

-- 铁匠系列  
('铁匠学徒', 17, 1.0, '铁匠', 1),
('铁匠', 5, 3.0, '百工专家', 2),

-- 医学系列
('医学生', 17, 1.0, '医师', 1),
('医师', 5, 3.0, '百工专家', 2),

-- 农民系列
('农民学徒', 17, 1.0, '农民', 1),
('农民', 5, 3.0, '百工专家', 2),

-- 艺术系列
('艺术学徒', 17, 1.0, '梦想家', 1),
('梦想家', 5, 3.0, '艺术家', 2),
('艺术家', 1, 9.0, NULL, 3),

-- 设计系列
('设计学徒', 17, 1.0, '设计师', 1),
('设计师', 5, 3.0, '艺术家', 2),

-- 军事系列
('兵卒', 17, 1.0, '老兵', 1),
('老兵', 5, 3.0, '统领', 2),
('统领', 1, 9.0, NULL, 3),

-- 道教系列
('道童', 17, 1.0, '道士', 1),
('道士', 5, 3.0, '住持', 2),
('住持', 1, 9.0, NULL, 3),

-- 佛教系列
('沙弥', 17, 1.0, '和尚', 1),
('和尚', 5, 3.0, '住持', 2),

-- 学术系列
('学生', 17, 1.0, '学者', 1),
('学者', 5, 3.0, '教授', 2),
('教授', 1, 9.0, NULL, 3),

-- 官员系列
('吏员', 17, 1.0, '官员', 1),
('官员', 5, 3.0, '相国', 2),
('相国', 1, 9.0, NULL, 3),

-- 商人系列
('小贩', 17, 1.0, '游商', 1),
('游商', 5, 3.0, '商人', 2),
('商人', 1, 9.0, NULL, 3);

-- 创建卡牌升级路径视图（可选）
CREATE VIEW card_upgrade_path AS
SELECT 
    c1.id as current_id,
    c1.name as current_name,
    c1.level as current_level,
    c1.damage as current_damage,
    c1.cards_num as current_cards_num,
    c2.id as target_id,
    c2.name as target_name,
    c2.level as target_level,
    c2.damage as target_damage,
    c2.cards_num as target_cards_num
FROM card_deck c1
LEFT JOIN card_deck c2 ON c1.targetname = c2.name
ORDER BY c1.level, c1.name;

-- 查询验证数据
SELECT 
    COUNT(*) as total_cards,
    COUNT(DISTINCT level) as total_levels,
    SUM(cards_num) as total_card_count
FROM card_deck;

-- 按等级统计卡牌
SELECT 
    level,
    COUNT(*) as card_types,
    SUM(cards_num) as total_cards,
    AVG(damage) as avg_damage
FROM card_deck 
GROUP BY level 
ORDER BY level;

-- 查看所有最高等级卡牌（终极卡牌）
SELECT name, damage, cards_num 
FROM card_deck 
WHERE targetname IS NULL 
ORDER BY damage DESC;

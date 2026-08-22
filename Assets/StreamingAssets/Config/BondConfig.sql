-- =============================================
-- 羁绊配置数据库创建脚本
-- 基于 BondConfig.json 生成
-- =============================================

-- 创建羁绊主表
CREATE TABLE IF NOT EXISTS bonds (
    id INT PRIMARY KEY COMMENT '羁绊ID',
    name VARCHAR(50) NOT NULL COMMENT '羁绊名称',
    level INT NOT NULL COMMENT '羁绊等级',
    damage DECIMAL(5,1) NOT NULL COMMENT '羁绊伤害',
    skill VARCHAR(100) DEFAULT '' COMMENT '羁绊技能',
    description VARCHAR(200) NOT NULL COMMENT '羁绊描述',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    INDEX idx_level (level),
    INDEX idx_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='羁绊配置表';

-- 创建羁绊卡牌关联表
CREATE TABLE IF NOT EXISTS bond_cards (
    id INT PRIMARY KEY COMMENT '关联ID',
    bond_id INT NOT NULL COMMENT '羁绊ID',
    card_name1 VARCHAR(50) DEFAULT NULL COMMENT '卡牌名称1',
    card_name2 VARCHAR(50) DEFAULT NULL COMMENT '卡牌名称2',
    card_name3 VARCHAR(50) DEFAULT NULL COMMENT '卡牌名称3',
    card_name4 VARCHAR(50) DEFAULT NULL COMMENT '卡牌名称4',
    card_name5 VARCHAR(50) DEFAULT NULL COMMENT '卡牌名称5',
    card_name6 VARCHAR(50) DEFAULT NULL COMMENT '卡牌名称6',
    card_name7 VARCHAR(50) DEFAULT NULL COMMENT '卡牌名称7',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    FOREIGN KEY (bond_id) REFERENCES bonds(id) ON DELETE CASCADE,
    INDEX idx_bond_id (bond_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='羁绊卡牌关联表';

-- 创建视图：包含卡牌信息的羁绊视图
CREATE OR REPLACE VIEW bond_details AS
SELECT 
    b.id,
    b.name,
    b.level,
    b.damage,
    b.skill,
    b.description,
    CONCAT_WS(',', 
        NULLIF(bc.card_name1, ''), 
        NULLIF(bc.card_name2, ''), 
        NULLIF(bc.card_name3, ''), 
        NULLIF(bc.card_name4, ''), 
        NULLIF(bc.card_name5, ''), 
        NULLIF(bc.card_name6, ''), 
        NULLIF(bc.card_name7, '')
    ) AS cards
FROM bonds b
LEFT JOIN bond_cards bc ON b.id = bc.bond_id
GROUP BY b.id, b.name, b.level, b.damage, b.skill, b.description
ORDER BY b.level, b.damage;

-- 清空现有数据
DELETE FROM bond_cards;
DELETE FROM bonds;

-- 插入羁绊数据
-- Level 1 羁绊 (12个)
INSERT INTO bonds (id, name, level, damage, skill, description) VALUES
-- 2张卡牌组成羁绊 (2个)
(1, '修行之道', 1, 5.0, '', '不同信仰的修行者'),
(2, '工匠初心', 1, 5.0, '', '工艺学徒的初始羁绊'),
-- 3张卡牌组成羁绊 (5个)
(3, '艺术启蒙', 1, 8.0, '', '艺术领域的学徒'),
(4, '劳动力量', 1, 8.0, '', '劳动与军事的基础'),
(5, '求学路', 1, 8.0, '', '求知的年轻人'),
(6, '服务民众', 1, 8.0, '', '为民服务的基层'),
(7, '医护之道', 1, 8.0, '', '关注生命的医者'),
-- 4张卡牌组成羁绊 (3个)
(8, '军政基础', 1, 12.0, '', '军事与政治的基础'),
(9, '文武之道', 1, 12.0, '', '文武并重'),
(10, '创意火花', 1, 12.0, '', '设计与学习的结合'),
-- 5张卡牌组成羁绊 (1个)
(11, '手工艺术', 1, 15.0, '', '手工与艺术的结合'),
-- 6张卡牌组成羁绊 (1个)
(12, '实用技能', 1, 18.0, '', '实用性技能');

-- Level 2 羁绊 (15个)
INSERT INTO bonds (id, name, level, damage, skill, description) VALUES
-- 2张卡牌组成羁绊 (5个)
(13, '工匠精神', 2, 20.0, '', '熟练工匠的精神'),
(14, '民生保障', 2, 20.0, '', '民生相关的保障'),
(15, '艺术传承', 2, 20.0, '', '艺术领域的传承'),
(16, '治理之道', 2, 20.0, '', '治国理政的智慧'),
(17, '信仰力量', 2, 20.0, '', '宗教信仰的力量'),
-- 3张卡牌组成羁绊 (5个)
(18, '商业网络', 2, 25.0, '', '商业与政治的结合'),
(19, '知识传播', 2, 25.0, '', '知识的传播与发展'),
(20, '军政联合', 2, 25.0, '', '军事与政治的联合'),
(21, '技艺融合', 2, 25.0, '', '不同技艺的融合'),
(22, '社会精英', 2, 25.0, '', '社会各界精英'),
-- 4张卡牌组成羁绊 (3个)
(23, '文武双修', 2, 30.0, '', '文武并重的修行'),
(24, '创新联盟', 2, 30.0, '', '创新技术的联盟'),
(25, '治世之道', 2, 30.0, '', '治理天下的智慧'),
-- 5张卡牌组成羁绊 (1个)
(26, '五行合一', 2, 35.0, '', '五大Level 2职业的完美结合'),
-- 6张卡牌组成羁绊 (1个)
(27, '民族复兴', 2, 40.0, '', '民族复兴的综合力量');

-- Level 3 羁绊 (5个)
INSERT INTO bonds (id, name, level, damage, skill, description) VALUES
-- 2张卡牌组成羁绊 (3个)
(28, '权力中枢', 3, 50.0, '', '国家权力的核心'),
(29, '精神领袖', 3, 50.0, '', '精神与智慧的领导者'),
(30, '商业帝国', 3, 50.0, '', '商业与政治的完美结合'),
-- 3张卡牌组成羁绊 (1个)
(31, '文武双全', 3, 60.0, '', '文武兼备的治世之才'),
-- 4张卡牌组成羁绊 (1个)
(32, '治世联盟', 3, 70.0, '', '治理天下的完美组合');

-- Level 4 羁绊 (3个)
INSERT INTO bonds (id, name, level, damage, skill, description) VALUES
-- 2张卡牌组成羁绊 (2个)
(33, '权势巅峰', 4, 80.0, '', '政治与军事的完美融合'),
(34, '至尊联盟', 4, 80.0, '', '政治与学术的至高联盟'),
-- 3张卡牌组成羁绊 (1个)
(35, '完美世界', 4, 100.0, '', '理想世界的完美体现');

-- 插入羁绊卡牌关联数据
-- Level 1 羁绊的卡牌关联 (只含等级1的卡牌)
INSERT INTO bond_cards (id, bond_id, card_name1, card_name2, card_name3, card_name4, card_name5, card_name6, card_name7) VALUES
-- 2张卡牌组成羁绊
(1, 1, '道童', '沙弥', NULL, NULL, NULL, NULL, NULL),
(2, 2, '木匠学徒', '铁匠学徒', NULL, NULL, NULL, NULL, NULL),
-- 3张卡牌组成羁绊
(3, 3, '艺术学徒', '设计学徒', '学生', NULL, NULL, NULL, NULL),
(4, 4, '农民学徒', '兵卒', '吏员', NULL, NULL, NULL, NULL),
(5, 5, '学生', '医学生', '道童', NULL, NULL, NULL, NULL),
(6, 6, '吏员', '小贩', '农民学徒', NULL, NULL, NULL, NULL),
(7, 7, '医学生', '道童', '沙弥', NULL, NULL, NULL, NULL),
-- 4张卡牌组成羁绊
(8, 8, '兵卒', '吏员', '学生', '木匠学徒', NULL, NULL, NULL),
(9, 9, '学生', '兵卒', '艺术学徒', '医学生', NULL, NULL, NULL),
(10, 10, '设计学徒', '学生', '艺术学徒', '小贩', NULL, NULL, NULL),
-- 5张卡牌组成羁绊
(11, 11, '木匠学徒', '艺术学徒', '铁匠学徒', '设计学徒', '农民学徒', NULL, NULL),
-- 6张卡牌组成羁绊
(12, 12, '铁匠学徒', '医学生', '兵卒', '道童', '吏员', '小贩', NULL);

-- Level 2 羁绊的卡牌关联 (至少包含一张等级2的卡牌)
INSERT INTO bond_cards (id, bond_id, card_name1, card_name2, card_name3, card_name4, card_name5, card_name6, card_name7) VALUES
-- 2张卡牌组成羁绊
(13, 13, '木匠', '木匠学徒', NULL, NULL, NULL, NULL, NULL),
(14, 14, '农民', '医学生', NULL, NULL, NULL, NULL, NULL),
(15, 15, '梦想家', '艺术学徒', NULL, NULL, NULL, NULL, NULL),
(16, 16, '官员', '吏员', NULL, NULL, NULL, NULL, NULL),
(17, 17, '道士', '道童', NULL, NULL, NULL, NULL, NULL),
-- 3张卡牌组成羁绊
(18, 18, '游商', '小贩', '官员', NULL, NULL, NULL, NULL),
(19, 19, '学者', '学生', '道士', NULL, NULL, NULL, NULL),
(20, 20, '老兵', '兵卒', '官员', NULL, NULL, NULL, NULL),
(21, 21, '木匠', '铁匠', '医学生', NULL, NULL, NULL, NULL),
(22, 22, '学者', '官员', '梦想家', NULL, NULL, NULL, NULL),
-- 4张卡牌组成羁绊
(23, 23, '学者', '老兵', '学生', '兵卒', NULL, NULL, NULL),
(24, 24, '设计师', '艺术学徒', '设计学徒', '木匠学徒', NULL, NULL, NULL),
(25, 25, '和尚', '道士', '道童', '沙弥', NULL, NULL, NULL),
-- 5张卡牌组成羁绊
(26, 26, '木匠', '铁匠', '农民', '医师', '道士', NULL, NULL),
-- 6张卡牌组成羁绊
(27, 27, '游商', '官员', '学者', '农民学徒', '兵卒', '医学生', NULL);

-- Level 3 羁绊的卡牌关联 (至少包含一张等级3的卡牌)
INSERT INTO bond_cards (id, bond_id, card_name1, card_name2, card_name3, card_name4, card_name5, card_name6, card_name7) VALUES
-- 2张卡牌组成羁绊
(28, 28, '相国', '官员', NULL, NULL, NULL, NULL, NULL),
(29, 29, '住持', '道士', NULL, NULL, NULL, NULL, NULL),
(30, 30, '商人', '游商', NULL, NULL, NULL, NULL, NULL),
-- 3张卡牌组成羁绊
(31, 31, '教授', '学者', '学生', NULL, NULL, NULL, NULL),
-- 4张卡牌组成羁绊
(32, 32, '统领', '教授', '住持', '商人', NULL, NULL, NULL);

-- Level 4 羁绊的卡牌关联 (至少包含两张等级3的卡牌)
INSERT INTO bond_cards (id, bond_id, card_name1, card_name2, card_name3, card_name4, card_name5, card_name6, card_name7) VALUES
-- 2张卡牌组成羁绊
(33, 33, '相国', '统领', NULL, NULL, NULL, NULL, NULL),
(34, 34, '教授', '住持', NULL, NULL, NULL, NULL, NULL),
-- 3张卡牌组成羁绊
(35, 35, '百工专家', '相国', '商人', NULL, NULL, NULL, NULL);

-- 创建查询统计信息的视图
CREATE OR REPLACE VIEW bond_statistics AS
SELECT 
    level,
    COUNT(*) as bond_count,
    MIN(damage) as min_damage,
    MAX(damage) as max_damage,
    AVG(damage) as avg_damage
FROM bonds 
GROUP BY level
ORDER BY level;

-- 查询验证数据
SELECT 'Bond Statistics:' as info;
SELECT * FROM bond_statistics;

SELECT 'Total Bonds by Level:' as info;
SELECT level, COUNT(*) as count FROM bonds GROUP BY level ORDER BY level;

SELECT 'Sample Bond Details:' as info;
SELECT * FROM bond_details LIMIT 10;

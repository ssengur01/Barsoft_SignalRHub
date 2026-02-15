-- =============================================
-- Barsoft SignalR Hub - Test Data Script
-- =============================================
-- Bu script test kullanıcıları ve örnek stok hareketleri oluşturur
-- Multi-tenant filtering testleri için farklı şube ID'leri kullanır
-- =============================================

USE BARSOFT;
GO

-- =============================================
-- 1. Test Users (TBL_USER_MAIN)
-- =============================================
PRINT 'Creating test users...';

-- User 1: Admin (Tüm şubelere erişim)
IF NOT EXISTS (SELECT 1 FROM TBL_USER_MAIN WHERE USERCODE = 'admin')
BEGIN
    INSERT INTO TBL_USER_MAIN (
        AKTIF, USERCODE, PASSWORD, DESCRIPTION, ADMIN, CREATEUSERCODE,
        SUBEIDS, KASAIDS, BANKAIDS
    )
    VALUES (
        1, -- Aktif
        'admin',
        '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIwNoH3jC2', -- BCrypt hash of 'password'
        'System Administrator',
        1, -- Admin
        'SYSTEM',
        '1,2,3,4,5', -- Tüm şubeler
        '1,2,3',
        '1,2'
    );
    PRINT '  ✓ Admin user created (SubeIds: 1,2,3,4,5)';
END

-- User 2: Branch 1 Manager
IF NOT EXISTS (SELECT 1 FROM TBL_USER_MAIN WHERE USERCODE = '0001')
BEGIN
    INSERT INTO TBL_USER_MAIN (
        AKTIF, USERCODE, PASSWORD, DESCRIPTION, ADMIN, CREATEUSERCODE,
        SUBEIDS
    )
    VALUES (
        1,
        '0001',
        '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIwNoH3jC2', -- 'password'
        'Branch 1 Manager',
        0,
        'admin',
        '1' -- Sadece şube 1
    );
    PRINT '  ✓ User 0001 created (SubeIds: 1)';
END

-- User 3: Branch 2 Manager
IF NOT EXISTS (SELECT 1 FROM TBL_USER_MAIN WHERE USERCODE = '0002')
BEGIN
    INSERT INTO TBL_USER_MAIN (
        AKTIF, USERCODE, PASSWORD, DESCRIPTION, ADMIN, CREATEUSERCODE,
        SUBEIDS
    )
    VALUES (
        1,
        '0002',
        '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIwNoH3jC2', -- 'password'
        'Branch 2 Manager',
        0,
        'admin',
        '2' -- Sadece şube 2
    );
    PRINT '  ✓ User 0002 created (SubeIds: 2)';
END

-- User 4: Multi-Branch User
IF NOT EXISTS (SELECT 1 FROM TBL_USER_MAIN WHERE USERCODE = '0003')
BEGIN
    INSERT INTO TBL_USER_MAIN (
        AKTIF, USERCODE, PASSWORD, DESCRIPTION, ADMIN, CREATEUSERCODE,
        SUBEIDS
    )
    VALUES (
        1,
        '0003',
        '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIwNoH3jC2', -- 'password'
        'Regional Manager',
        0,
        'admin',
        '1,2,3' -- Şube 1, 2, 3
    );
    PRINT '  ✓ User 0003 created (SubeIds: 1,2,3)';
END

-- User 5: Inactive User (Test için)
IF NOT EXISTS (SELECT 1 FROM TBL_USER_MAIN WHERE USERCODE = 'inactive')
BEGIN
    INSERT INTO TBL_USER_MAIN (
        AKTIF, USERCODE, PASSWORD, DESCRIPTION, ADMIN, CREATEUSERCODE,
        SUBEIDS
    )
    VALUES (
        0, -- Pasif
        'inactive',
        '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIwNoH3jC2',
        'Inactive User',
        0,
        'admin',
        '1'
    );
    PRINT '  ✓ Inactive user created (for testing)';
END

PRINT '';
PRINT 'Test users created successfully!';
PRINT 'Default password for all users: password';
PRINT '';

-- =============================================
-- 2. Sample Stock Movements (TBL_STOK_HAREKET)
-- =============================================
PRINT 'Creating sample stock movements...';

-- Şube 1 Hareketleri
DECLARE @BranchCount INT = 0;

-- Branch 1: 5 sample movements
INSERT INTO TBL_STOK_HAREKET (
    STOKID, BELGEKODU, BELGETARIHI, MIKTAR, TOPLAMTUTAR,
    CREATEDATE, CREATEUSERID, MASRAFMERKEZIID,
    BIRIMID, BIRIMCARPAN, BIRIMFIYATI, DEPOID, KDV, DOVIZID,
    DOVIZTUTARI, KDVTUTARI, INDIRIMTUTARI, ARTIRIMTUTARI,
    DETAYID, ACIKLAMA, HAREKETTIPID
)
SELECT
    1000 + n, -- STOKID
    'BRANCH1-' + RIGHT('00' + CAST(n AS VARCHAR), 3), -- BELGEKODU
    DATEADD(DAY, -n, GETDATE()), -- BELGETARIHI
    10.0 * n, -- MIKTAR
    100.0 * n, -- TOPLAMTUTAR
    DATEADD(DAY, -n, GETDATE()), -- CREATEDATE
    1, -- CREATEUSERID
    1, -- MASRAFMERKEZIID = 1 (Branch 1)
    1, 1.0, 10.0, 1, 18.0, 1,
    100.0 * n, 18.0 * n, 0.0, 0.0,
    1, 'Test Stock Movement - Branch 1 - ' + CAST(n AS VARCHAR), 1
FROM (SELECT 1 AS n UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5) AS Numbers;

SET @BranchCount = @@ROWCOUNT;
PRINT '  ✓ ' + CAST(@BranchCount AS VARCHAR) + ' movements created for Branch 1 (MasrafMerkeziId=1)';

-- Branch 2: 5 sample movements
INSERT INTO TBL_STOK_HAREKET (
    STOKID, BELGEKODU, BELGETARIHI, MIKTAR, TOPLAMTUTAR,
    CREATEDATE, CREATEUSERID, MASRAFMERKEZIID,
    BIRIMID, BIRIMCARPAN, BIRIMFIYATI, DEPOID, KDV, DOVIZID,
    DOVIZTUTARI, KDVTUTARI, INDIRIMTUTARI, ARTIRIMTUTARI,
    DETAYID, ACIKLAMA, HAREKETTIPID
)
SELECT
    2000 + n,
    'BRANCH2-' + RIGHT('00' + CAST(n AS VARCHAR), 3),
    DATEADD(DAY, -n, GETDATE()),
    15.0 * n,
    150.0 * n,
    DATEADD(DAY, -n, GETDATE()),
    2,
    2, -- MASRAFMERKEZIID = 2 (Branch 2)
    1, 1.0, 10.0, 1, 18.0, 1,
    150.0 * n, 27.0 * n, 0.0, 0.0,
    1, 'Test Stock Movement - Branch 2 - ' + CAST(n AS VARCHAR), 1
FROM (SELECT 1 AS n UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5) AS Numbers;

SET @BranchCount = @@ROWCOUNT;
PRINT '  ✓ ' + CAST(@BranchCount AS VARCHAR) + ' movements created for Branch 2 (MasrafMerkeziId=2)';

-- Branch 3: 5 sample movements
INSERT INTO TBL_STOK_HAREKET (
    STOKID, BELGEKODU, BELGETARIHI, MIKTAR, TOPLAMTUTAR,
    CREATEDATE, CREATEUSERID, MASRAFMERKEZIID,
    BIRIMID, BIRIMCARPAN, BIRIMFIYATI, DEPOID, KDV, DOVIZID,
    DOVIZTUTARI, KDVTUTARI, INDIRIMTUTARI, ARTIRIMTUTARI,
    DETAYID, ACIKLAMA, HAREKETTIPID
)
SELECT
    3000 + n,
    'BRANCH3-' + RIGHT('00' + CAST(n AS VARCHAR), 3),
    DATEADD(DAY, -n, GETDATE()),
    20.0 * n,
    200.0 * n,
    DATEADD(DAY, -n, GETDATE()),
    3,
    3, -- MASRAFMERKEZIID = 3 (Branch 3)
    1, 1.0, 10.0, 1, 18.0, 1,
    200.0 * n, 36.0 * n, 0.0, 0.0,
    1, 'Test Stock Movement - Branch 3 - ' + CAST(n AS VARCHAR), 1
FROM (SELECT 1 AS n UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5) AS Numbers;

SET @BranchCount = @@ROWCOUNT;
PRINT '  ✓ ' + CAST(@BranchCount AS VARCHAR) + ' movements created for Branch 3 (MasrafMerkeziId=3)';

PRINT '';
PRINT 'Sample stock movements created successfully!';
PRINT '';

-- =============================================
-- 3. Test Data Summary
-- =============================================
PRINT '==============================================';
PRINT 'TEST DATA SUMMARY';
PRINT '==============================================';
PRINT '';

PRINT 'Users:';
SELECT
    USERCODE,
    DESCRIPTION,
    CASE WHEN ADMIN = 1 THEN 'Yes' ELSE 'No' END AS IsAdmin,
    CASE WHEN AKTIF = 1 THEN 'Active' ELSE 'Inactive' END AS Status,
    ISNULL(SUBEIDS, 'N/A') AS SubeIds
FROM TBL_USER_MAIN
WHERE USERCODE IN ('admin', '0001', '0002', '0003', 'inactive')
ORDER BY USERCODE;

PRINT '';
PRINT 'Stock Movements by Branch:';
SELECT
    MASRAFMERKEZIID AS BranchId,
    COUNT(*) AS MovementCount,
    SUM(MIKTAR) AS TotalQuantity,
    SUM(TOPLAMTUTAR) AS TotalAmount
FROM TBL_STOK_HAREKET
WHERE BELGEKODU LIKE 'BRANCH%'
GROUP BY MASRAFMERKEZIID
ORDER BY MASRAFMERKEZIID;

PRINT '';
PRINT '==============================================';
PRINT 'TEST CREDENTIALS';
PRINT '==============================================';
PRINT 'Username: admin     | Password: password | Access: All branches';
PRINT 'Username: 0001      | Password: password | Access: Branch 1';
PRINT 'Username: 0002      | Password: password | Access: Branch 2';
PRINT 'Username: 0003      | Password: password | Access: Branch 1,2,3';
PRINT 'Username: inactive  | Password: password | Access: Disabled';
PRINT '==============================================';
PRINT '';
PRINT 'Test data creation completed! ✓';
GO

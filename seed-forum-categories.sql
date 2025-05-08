-- Check if the ForumCategories table exists and has records
SET @category_count = (SELECT COUNT(*) FROM ForumCategories);

-- Insert forum categories if table is empty
INSERT INTO ForumCategories (Name, Description, DisplayOrder, IsActive, CreatedAt)
VALUES 
    ('General Discussion', 'Discuss any general topics related to our community', 1, TRUE, NOW()),
    ('Announcements', 'Important community announcements and updates', 2, TRUE, NOW()),
    ('Events & Activities', 'Discuss upcoming events and activities in our neighborhood', 3, TRUE, NOW()),
    ('Facilities', 'Discussions about our community facilities and amenities', 4, TRUE, NOW()),
    ('Help & Support', 'Ask questions and get help from community members', 5, TRUE, NOW());

SELECT 'Forum categories have been added.' as Message; 
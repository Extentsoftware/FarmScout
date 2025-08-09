-- ============================================================================
-- REPORT TABLES FOR MARKDOWN REPORTS
-- ============================================================================

-- Markdown Reports table
CREATE TABLE markdown_reports (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(255) NOT NULL,
    report_group VARCHAR(100) NOT NULL,
    date_produced TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    report_markdown TEXT NOT NULL,
    file_name VARCHAR(255),
    file_size BIGINT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    -- Add a unique constraint to prevent duplicate reports with same title and group
    CONSTRAINT unique_report_title_group UNIQUE (title, report_group, date_produced)
);

-- Report Groups lookup table (optional - for standardized report groups)
CREATE TABLE report_groups (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) UNIQUE NOT NULL,
    description TEXT,
    icon VARCHAR(10) DEFAULT '📊',
    color VARCHAR(7) DEFAULT '#607D8B',
    sort_order INTEGER DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================================================
-- INDEXES FOR REPORT TABLES
-- ============================================================================

-- Markdown Reports indexes
CREATE INDEX idx_markdown_reports_title ON markdown_reports(title);
CREATE INDEX idx_markdown_reports_report_group ON markdown_reports(report_group);
CREATE INDEX idx_markdown_reports_date_produced ON markdown_reports(date_produced DESC);
CREATE INDEX idx_markdown_reports_is_active ON markdown_reports(is_active);
CREATE INDEX idx_markdown_reports_created_at ON markdown_reports(created_at DESC);

-- Report Groups indexes
CREATE INDEX idx_report_groups_name ON report_groups(name);
CREATE INDEX idx_report_groups_is_active ON report_groups(is_active);
CREATE INDEX idx_report_groups_sort_order ON report_groups(sort_order);

-- ============================================================================
-- TRIGGERS FOR REPORT TABLES
-- ============================================================================

-- Create triggers for updated_at columns
CREATE TRIGGER update_markdown_reports_updated_at 
    BEFORE UPDATE ON markdown_reports 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_report_groups_updated_at 
    BEFORE UPDATE ON report_groups 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- SAMPLE DATA FOR REPORT GROUPS
-- ============================================================================

-- Insert common report groups
INSERT INTO report_groups (name, description, icon, color, sort_order) VALUES
('Moisture Reports', 'Soil moisture analysis and monitoring reports', '💧', '#00BCD4', 1),
('Section Health', 'Overall section health and condition reports', '🏥', '#4CAF50', 2),
('Harvest Reports', 'Harvest yield and quality analysis', '🌾', '#FFC107', 3),
('Weather Reports', 'Weather condition and impact analysis', '🌤️', '#2196F3', 4),
('Pest Reports', 'Pest infestation and control reports', '🐛', '#FF9800', 5),
('Disease Reports', 'Plant disease monitoring and treatment', '🦠', '#F44336', 6),
('Soil Reports', 'Soil condition and nutrient analysis', '🌍', '#8D6E63', 7),
('Growth Reports', 'Plant growth and development tracking', '📈', '#9C27B0', 8);

-- ============================================================================
-- VIEWS FOR REPORT QUERIES
-- ============================================================================

-- View for active reports with group information
CREATE VIEW active_reports_with_groups AS
SELECT 
    mr.id,
    mr.title,
    mr.report_group,
    rg.description as group_description,
    rg.icon as group_icon,
    rg.color as group_color,
    mr.date_produced,
    mr.file_name,
    mr.file_size,
    mr.created_at,
    mr.updated_at
FROM markdown_reports mr
LEFT JOIN report_groups rg ON mr.report_group = rg.name
WHERE mr.is_active = true
ORDER BY mr.date_produced DESC;

-- View for report statistics by group
CREATE VIEW report_statistics_by_group AS
SELECT 
    report_group,
    COUNT(*) as total_reports,
    COUNT(*) FILTER (WHERE date_produced >= NOW() - INTERVAL '30 days') as reports_last_30_days,
    COUNT(*) FILTER (WHERE date_produced >= NOW() - INTERVAL '7 days') as reports_last_7_days,
    MIN(date_produced) as first_report_date,
    MAX(date_produced) as latest_report_date,
    AVG(file_size) as avg_file_size,
    SUM(file_size) as total_file_size
FROM markdown_reports
WHERE is_active = true
GROUP BY report_group
ORDER BY total_reports DESC;

-- ============================================================================
-- FUNCTIONS FOR REPORT MANAGEMENT
-- ============================================================================

-- Function to get reports by group with pagination
CREATE OR REPLACE FUNCTION get_reports_by_group(
    group_name VARCHAR(100),
    limit_count INTEGER DEFAULT 10,
    offset_count INTEGER DEFAULT 0
)
RETURNS TABLE (
    report_id UUID,
    report_title VARCHAR(255),
    report_date TIMESTAMP WITH TIME ZONE,
    file_name VARCHAR(255),
    file_size BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        mr.id,
        mr.title,
        mr.date_produced,
        mr.file_name,
        mr.file_size
    FROM markdown_reports mr
    WHERE mr.report_group = group_name 
        AND mr.is_active = true
    ORDER BY mr.date_produced DESC
    LIMIT limit_count
    OFFSET offset_count;
END;
$$ LANGUAGE plpgsql;

-- Function to search reports by title or content
CREATE OR REPLACE FUNCTION search_reports(
    search_term TEXT,
    limit_count INTEGER DEFAULT 20
)
RETURNS TABLE (
    report_id UUID,
    report_title VARCHAR(255),
    report_group VARCHAR(100),
    report_date TIMESTAMP WITH TIME ZONE,
    relevance_score INTEGER
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        mr.id,
        mr.title,
        mr.report_group,
        mr.date_produced,
        CASE 
            WHEN mr.title ILIKE '%' || search_term || '%' THEN 3
            WHEN mr.report_markdown ILIKE '%' || search_term || '%' THEN 1
            ELSE 0
        END as relevance_score
    FROM markdown_reports mr
    WHERE mr.is_active = true
        AND (mr.title ILIKE '%' || search_term || '%' 
             OR mr.report_markdown ILIKE '%' || search_term || '%')
    ORDER BY relevance_score DESC, mr.date_produced DESC
    LIMIT limit_count;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- COMMENTS FOR DOCUMENTATION
-- ============================================================================

COMMENT ON TABLE markdown_reports IS 'Stores markdown reports generated by backend systems';
COMMENT ON TABLE report_groups IS 'Lookup table for standardized report group categories';
COMMENT ON VIEW active_reports_with_groups IS 'View combining reports with their group information';
COMMENT ON VIEW report_statistics_by_group IS 'Statistical summary of reports by group';
COMMENT ON FUNCTION get_reports_by_group IS 'Retrieve reports for a specific group with pagination';
COMMENT ON FUNCTION search_reports IS 'Search reports by title or content with relevance scoring'; 
-- ============================================================================
-- METADATA TABLES
-- ============================================================================

-- Observation Metadata table
CREATE TABLE observation_metadata (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    observation_id UUID NOT NULL,
    observation_type_id UUID NOT NULL,
    data_point_id UUID NOT NULL,
    value TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT fk_observation_metadata_observation 
        FOREIGN KEY (observation_id) REFERENCES observations(id) 
        ON DELETE CASCADE,
    CONSTRAINT fk_observation_metadata_observation_type 
        FOREIGN KEY (observation_type_id) REFERENCES observation_types(id) 
        ON DELETE CASCADE,
    CONSTRAINT fk_observation_metadata_data_point 
        FOREIGN KEY (data_point_id) REFERENCES observation_type_data_points(id) 
        ON DELETE CASCADE
);

-- ============================================================================
-- REPORT TABLES
-- ============================================================================

-- Markdown Reports table
CREATE TABLE markdown_reports (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(255) NOT NULL,
    report_group_id UUID NOT NULL,
    date_produced TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    report_markdown TEXT NOT NULL,
    file_name VARCHAR(255),
    file_size BIGINT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    -- Add a unique constraint to prevent duplicate reports with same title and group
    CONSTRAINT unique_report_title_group UNIQUE (title, report_group_id, date_produced),
    
    -- Foreign key constraint to report_groups table
    CONSTRAINT fk_markdown_reports_report_group 
        FOREIGN KEY (report_group_id) REFERENCES report_groups(id) 
        ON DELETE RESTRICT
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
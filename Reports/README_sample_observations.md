# Sample Observations for FarmScout

This directory contains sample observations that have been generated to demonstrate the FarmScout application's capabilities across all observation types.

## Files Generated

### 1. `sample_observations.csv`
- **200 sample observations** across all observation types
- **Date range**: January 19, 2025 to July 17, 2025 (6 months)
- **Sections**: All 11 farm sections (B1S1, B1S2, B2S1A, B2S1B, B2S2, B2S3, B2S4, B2S5, B2S6, B2S7, B2S8)
- **Metrics**: 38 unique metrics including all MAUI observation types

### 2. `combined_observations.csv`
- **2,617 total observations** (2,417 original + 200 sample)
- **Date range**: November 20, 2024 to July 17, 2025
- **Complete dataset** showing how new observations integrate with existing data

## Observation Types Covered

### MAUI App Observation Types (9 types)
All 9 observation types from the FarmScout MAUI application are included:

1. **Disease** 🦠 - Plant disease observations
2. **Dead Plant** 💀 - Dead or dying plant observations  
3. **Pest** 🐛 - Pest infestation observations
4. **Damage** 💥 - Plant damage observations
5. **Growth** 🌱 - Plant growth observations
6. **Harvest** 🌾 - Harvest observations
7. **Weather** 🌤️ - Weather condition observations
8. **Soil** 🌍 - Soil condition observations
9. **Soil Moisture** 💧 - Soil moisture observations

### Traditional Metrics (29 types)
All existing metrics from the original dataset are preserved:
- Insect pests, diseases, moisture, mowing, etc.
- Basin management, irrigation, plant health
- Growth stages, harvesting, maintenance

## Data Distribution

### Condition Distribution
- **Pass**: 58.0% (116 observations)
- **Partial**: 32.0% (64 observations)  
- **Fail**: 10.0% (20 observations)

### Section Distribution
- **B1S2**: 26 observations (13.0%)
- **B2S1B**: 25 observations (12.5%)
- **B2S2**: 25 observations (12.5%)
- **B2S6**: 19 observations (9.5%)
- **B2S5**: 18 observations (9.0%)
- **B2S8**: 17 observations (8.5%)
- **B2S3**: 17 observations (8.5%)
- **B1S1**: 15 observations (7.5%)
- **B2S1A**: 14 observations (7.0%)
- **B2S7**: 12 observations (6.0%)
- **B2S4**: 12 observations (6.0%)

## Analysis Files

### Dashboards
- `sample_observations_dashboard.png` - Analysis of sample observations
- `combined_datasets_dashboard.png` - Comparison of original vs sample data

### Reports
- `sample_observations_report.txt` - Detailed analysis of sample data
- `combined_datasets_report.txt` - Comprehensive comparison report

## Scripts

### 1. `generate_sample_observations.py`
Generates 200 realistic sample observations with:
- Realistic date distribution over 6 months
- Proper condition weighting (60% pass, 30% partial, 10% fail)
- Contextual notes based on metric type
- Coverage of all MAUI observation types

### 2. `analyze_sample_observations.py`
Creates comprehensive analysis including:
- Condition distribution analysis
- Section activity mapping
- MAUI vs traditional metrics comparison
- Performance scoring for observation types
- Timeline analysis

### 3. `combine_datasets.py`
Combines original and sample data to show:
- Dataset integration
- Metric coverage expansion
- Timeline continuity
- Performance comparison

## Usage Examples

### 1. Testing MAUI App Features
The sample data includes all 9 MAUI observation types, making it perfect for:
- Testing observation type workflows
- Validating data point configurations
- Demonstrating metadata capture
- Testing reporting features

### 2. Data Analysis
The combined dataset (2,617 observations) provides:
- Sufficient data for meaningful analytics
- Realistic patterns for trend analysis
- Diverse scenarios for testing algorithms
- Comprehensive coverage for dashboard development

### 3. Development and Testing
- **Unit Testing**: Use sample data for automated tests
- **Integration Testing**: Test data flow between components
- **Performance Testing**: Large dataset for stress testing
- **User Acceptance Testing**: Realistic scenarios for user testing

## Data Quality Features

### Realistic Patterns
- **Seasonal variation** in observation types
- **Realistic condition distributions** based on farm management
- **Contextual notes** that match observation types
- **Proper date sequencing** for timeline analysis

### Comprehensive Coverage
- **All sections** represented
- **All observation types** included
- **Diverse conditions** and scenarios
- **Realistic metadata** and notes

### Integration Ready
- **Compatible format** with existing data
- **Consistent structure** for seamless integration
- **Proper date formatting** for analysis
- **Clean data** ready for processing

## Next Steps

1. **Import into MAUI App**: Use the sample data to test the FarmScout application
2. **Generate Reports**: Create dashboards and reports using the combined dataset
3. **Test Features**: Validate all observation type workflows
4. **Performance Analysis**: Use the larger dataset for performance testing
5. **User Training**: Use realistic data for user training and demonstrations

## File Structure

```
Reports/
├── sample_observations.csv          # 200 sample observations
├── combined_observations.csv        # 2,617 total observations
├── generate_sample_observations.py  # Generation script
├── analyze_sample_observations.py   # Analysis script
├── combine_datasets.py              # Combination script
├── sample_observations_dashboard.png # Sample data dashboard
├── combined_datasets_dashboard.png  # Combined data dashboard
├── sample_observations_report.txt   # Sample data report
├── combined_datasets_report.txt     # Combined data report
└── README_sample_observations.md    # This file
```

This sample dataset provides a comprehensive foundation for testing, development, and demonstration of the FarmScout application's capabilities. 
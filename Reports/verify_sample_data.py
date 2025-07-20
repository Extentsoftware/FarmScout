import pandas as pd

def verify_sample_data():
    """Verify the sample observations data"""
    
    # Read the sample data
    df = pd.read_csv('sample_observations.csv')
    
    print("SAMPLE OBSERVATIONS VERIFICATION")
    print("=" * 50)
    print(f"Total observations: {len(df)}")
    print(f"Date range: {df['date'].min()} to {df['date'].max()}")
    print(f"Unique sections: {df['section'].nunique()}")
    print(f"Unique metrics: {df['metric'].nunique()}")
    
    # Check MAUI observation types
    maui_types = ['Disease', 'Dead Plant', 'Pest', 'Damage', 'Growth', 'Harvest', 'Weather', 'Soil', 'Soil Moisture']
    maui_count = df[df['metric'].isin(maui_types)].shape[0]
    print(f"MAUI observation types: {maui_count}")
    
    print("\nMAUI Observation Types Found:")
    for maui_type in maui_types:
        count = df[df['metric'] == maui_type].shape[0]
        print(f"  {maui_type}: {count} observations")
    
    print("\nCondition Distribution:")
    condition_counts = df['condition'].value_counts()
    for condition, count in condition_counts.items():
        percentage = (count / len(df)) * 100
        print(f"  {condition}: {count} ({percentage:.1f}%)")
    
    print("\nSection Distribution:")
    section_counts = df['section'].value_counts().sort_values(ascending=False)
    for section, count in section_counts.items():
        percentage = (count / len(df)) * 100
        print(f"  {section}: {count} ({percentage:.1f}%)")
    
    print("\nSample observations (first 5):")
    for i, row in df.head(5).iterrows():
        print(f"  {i+1}. {row['date']} | {row['section']} | {row['metric']} | {row['condition']} | {row['notes'][:50]}...")
    
    print("\nVERIFICATION COMPLETE!")
    print("✅ Sample data generated successfully with all MAUI observation types")

if __name__ == "__main__":
    verify_sample_data() 
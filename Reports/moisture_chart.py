import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
from datetime import datetime
import numpy as np

def parse_date(date_str):
    """Parse date string to datetime object"""
    try:
        return datetime.strptime(date_str, '%d/%m/%Y')
    except:
        return None

def condition_to_numeric(condition):
    """Convert condition to numeric value for plotting"""
    condition_map = {
        'pass': 3,
        'partial': 2,
        'fail': 1,
        'n/a': 0
    }
    return condition_map.get(condition.lower(), 0)

def create_moisture_chart():
    """Create chart showing soil moisture conditions over time by section"""
    
    # Read the CSV file
    df = pd.read_csv('scout.csv')
    
    # Filter for moisture metrics only
    moisture_df = df[df['metric'].str.lower() == 'moisture'].copy()
    
    if moisture_df.empty:
        print("No moisture data found in the CSV file.")
        return
    
    # Parse dates
    moisture_df['date'] = moisture_df['date'].apply(parse_date)
    
    # Remove rows with invalid dates
    moisture_df = moisture_df.dropna(subset=['date'])
    
    # Convert conditions to numeric values
    moisture_df['condition_numeric'] = moisture_df['condition'].apply(condition_to_numeric)
    
    # Create the plot
    plt.figure(figsize=(15, 10))
    
    # Get unique sections
    sections = moisture_df['section'].unique()
    colors = plt.cm.Set3(np.linspace(0, 1, len(sections)))
    
    # Plot each section
    for i, section in enumerate(sections):
        section_data = moisture_df[moisture_df['section'] == section].sort_values('date')
        if not section_data.empty:
            plt.plot(section_data['date'], section_data['condition_numeric'], 
                    marker='o', linewidth=2, markersize=6, label=section, color=colors[i])
    
    # Customize the plot
    plt.title('Soil Moisture Conditions Over Time by Section', fontsize=16, fontweight='bold')
    plt.xlabel('Date', fontsize=12)
    plt.ylabel('Moisture Condition', fontsize=12)
    
    # Set y-axis labels
    plt.yticks([1, 2, 3], ['Fail', 'Partial', 'Pass'])
    plt.ylim(0.5, 3.5)
    
    # Rotate x-axis labels for better readability
    plt.xticks(rotation=45)
    
    # Add grid
    plt.grid(True, alpha=0.3)
    
    # Add legend
    plt.legend(bbox_to_anchor=(1.05, 1), loc='upper left')
    
    # Adjust layout to prevent label cutoff
    plt.tight_layout()
    
    # Save the plot
    plt.savefig('moisture_conditions_chart.png', dpi=300, bbox_inches='tight')
    print("Chart saved as 'moisture_conditions_chart.png'")
    
    # Show the plot
    plt.show()
    
    # Print summary statistics
    print("\n=== Moisture Data Summary ===")
    print(f"Total moisture observations: {len(moisture_df)}")
    print(f"Date range: {moisture_df['date'].min().strftime('%Y-%m-%d')} to {moisture_df['date'].max().strftime('%Y-%m-%d')}")
    print(f"Sections monitored: {len(sections)}")
    
    print("\n=== Condition Distribution ===")
    condition_counts = moisture_df['condition'].value_counts()
    for condition, count in condition_counts.items():
        print(f"{condition}: {count} observations")
    
    print("\n=== Section Summary ===")
    for section in sections:
        section_data = moisture_df[moisture_df['section'] == section]
        print(f"\n{section}:")
        print(f"  Total observations: {len(section_data)}")
        print(f"  Average condition: {section_data['condition'].mode().iloc[0] if not section_data.empty else 'N/A'}")
        print(f"  Date range: {section_data['date'].min().strftime('%Y-%m-%d')} to {section_data['date'].max().strftime('%Y-%m-%d')}")

if __name__ == "__main__":
    create_moisture_chart() 
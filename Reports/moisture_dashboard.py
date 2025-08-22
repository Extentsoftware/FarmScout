import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
from datetime import datetime, timedelta
import numpy as np
from matplotlib.patches import Rectangle
import warnings
warnings.filterwarnings('ignore')

# Set style for better visualizations
plt.style.use('default')
sns.set_palette("husl")

def parse_date(date_str):
    """Parse date string to datetime object - always as dd/mm/yyyy"""
    if pd.isna(date_str) or str(date_str).strip() == '':
        return None
    
    date_str = str(date_str).strip()
    
    # Try dd/mm/yyyy format first
    try:
        return datetime.strptime(date_str, '%d/%m/%Y')
    except ValueError:
        pass
    
    # Try dd/mm/yy format
    try:
        return datetime.strptime(date_str, '%d/%m/%y')
    except ValueError:
        pass
    
    # Try dd-mm-yyyy format
    try:
        return datetime.strptime(date_str, '%d-%m-%Y')
    except ValueError:
        pass
    
    # Try dd-mm-yy format
    try:
        return datetime.strptime(date_str, '%d-%m-%y')
    except ValueError:
        pass
    
    # If all else fails, try to extract date components
    try:
        # Look for patterns like "dd/mm/yyyy" or "dd-mm-yyyy"
        import re
        date_pattern = r'(\d{1,2})[/-](\d{1,2})[/-](\d{2,4})'
        match = re.search(date_pattern, date_str)
        if match:
            day, month, year = match.groups()
            day, month = int(day), int(month)
            year = int(year)
            if year < 100:  # Assume 20xx for 2-digit years
                year += 2000
            return datetime(year, month, day)
    except:
        pass
    
    print(f"Warning: Could not parse date: {date_str}")
    return None

def condition_to_numeric(condition):
    """Convert condition to numeric value for analysis"""
    condition_map = {
        'pass': 3,
        'partial': 2,
        'fail': 1,
        'n/a': 0
    }
    return condition_map.get(condition.lower(), 0)

def format_date_for_display(date_obj):
    """Format date object to dd/mm/yyyy string"""
    if date_obj is None:
        return "Unknown"
    return date_obj.strftime('%d/%m/%Y')

def create_moisture_dashboard():
    """Create comprehensive dashboard showing recent soil moisture conditions"""
    
    # Read the CSV file
    df = pd.read_csv('scout.csv')
    
    # Rename columns to match expected names
    df = df.rename(columns={
        'Date': 'date',
        'Section': 'section', 
        'Observation Type': 'metric',
        'Pass/Fail': 'condition',
        'Scout': 'scout',
        'Notes': 'notes'
    })
    
    # Parse dates
    df['date'] = df['date'].apply(parse_date)
    df = df.dropna(subset=['date'])
    
    # Filter for moisture metrics only
    moisture_df = df[df['metric'].str.lower() == 'soil moisture'].copy()
    
    if moisture_df.empty:
        print("No moisture data found in the CSV file.")
        return
    
    # Convert conditions to numeric values
    moisture_df['condition_numeric'] = moisture_df['condition'].apply(condition_to_numeric)
    
    # Get the most recent moisture data for each section
    latest_moisture = moisture_df.loc[moisture_df.groupby('section')['date'].idxmax()]
    
    # Create the dashboard
    fig = plt.figure(figsize=(6, 40))
    
    # Set up the grid
    gs = fig.add_gridspec(5, 1, hspace=0.5, wspace=0.3, height_ratios=[1, 1.5, 1.5, 1, 2])
    
    # 2. Current Moisture Status Heatmap
    ax1 = fig.add_subplot(gs[0, :])
    
    # Create heatmap data
    section_scores = latest_moisture.groupby('section')['condition_numeric'].first()
    heatmap_data = section_scores.values.reshape(1, -1)
    
    # Create custom colormap
    colors = ['red', 'orange', 'green']
    n_bins = 3
    cmap = plt.cm.colors.ListedColormap(colors)
    
    
    im = ax1.imshow(heatmap_data, cmap=cmap, aspect='auto', vmin=0.5, vmax=3.5)
    ax1.set_yticks([])
    ax1.set_xticks(range(len(section_scores)))
    ax1.set_xticklabels(section_scores.index, rotation=45, ha='right', fontsize=9)
    ax1.set_title('Current Moisture Status Heatmap', fontsize=12, fontweight='bold')
    
    # Add value annotations
    for i, score in enumerate(section_scores.values):
        color = 'white' if score < 2 else 'black'
        ax1.text(i, 0, f'{score:.1f}', ha='center', va='center', 
                color=color, fontweight='bold', fontsize=10)
    
    # 3. Moisture Trend Analysis (Last 60 days)
    ax2 = fig.add_subplot(gs[1, :])
    
    # Get data from last 60 days
    latest_date = moisture_df['date'].max()
    sixty_days_ago = latest_date - timedelta(days=60)
    recent_moisture = moisture_df[moisture_df['date'] >= sixty_days_ago]
    
    if not recent_moisture.empty:
        # Calculate trend for each section
        section_trends = []
        for section in recent_moisture['section'].unique():
            section_data = recent_moisture[recent_moisture['section'] == section].sort_values('date')
            if len(section_data) >= 2:
                # Calculate trend (positive = improving, negative = worsening)
                first_score = section_data['condition_numeric'].iloc[0]
                last_score = section_data['condition_numeric'].iloc[-1]
                trend = last_score - first_score
                section_trends.append({'section': section, 'trend': trend, 'current': last_score})
        
        if section_trends:
            trend_df = pd.DataFrame(section_trends)
            trend_df = trend_df.sort_values('trend')
            
            colors = ['red' if t < -0.5 else 'orange' if t < 0.5 else 'green' for t in trend_df['trend']]
            bars = ax2.barh(range(len(trend_df)), trend_df['trend'], color=colors)
            
            ax2.set_yticks(range(len(trend_df)))
            ax2.set_yticklabels(trend_df['section'], fontsize=9)
            ax2.set_xlabel('Trend (Negative = Worsening, Positive = Improving)')
            ax2.set_title('Moisture Trend Analysis (Last 60 Days)', fontsize=12, fontweight='bold')
            ax2.axvline(x=0, color='black', linestyle='--', alpha=0.5)
            ax2.grid(True, alpha=0.3)
    

    
    # 5. Section Monitoring Activity
    ax4 = fig.add_subplot(gs[2, :])
    
    section_activity = moisture_df.groupby('section').size().sort_values(ascending=True)
    
    bars = ax4.barh(range(len(section_activity)), section_activity.values, 
                    color=plt.cm.viridis(np.linspace(0, 1, len(section_activity))))
    ax4.set_yticks(range(len(section_activity)))
    ax4.set_yticklabels(section_activity.index, fontsize=9)
    ax4.set_xlabel('Number of Observations')
    ax4.set_title('Section Monitoring Activity', fontsize=12, fontweight='bold')
    
    # Add value labels
    for i, bar in enumerate(bars):
        width = bar.get_width()
        ax4.text(width + 0.1, bar.get_y() + bar.get_height()/2, 
                f'{int(width)}', ha='left', va='center', fontsize=8)
    
    # 6. Time Series of Moisture Conditions
    ax5 = fig.add_subplot(gs[3, :])
    
    # Get monthly averages
    moisture_df['month'] = moisture_df['date'].dt.to_period('M')
    monthly_avg = moisture_df.groupby('month')['condition_numeric'].mean().reset_index()
    monthly_avg['month'] = monthly_avg['month'].astype(str)
    
    ax5.plot(range(len(monthly_avg)), monthly_avg['condition_numeric'], 
             marker='o', linewidth=2, markersize=8, color='blue')
    ax5.set_xticks(range(len(monthly_avg)))
    ax5.set_xticklabels(monthly_avg['month'], rotation=45, ha='right')
    ax5.set_ylabel('Average Moisture Score')
    ax5.set_title('Monthly Average Moisture Conditions', fontsize=12, fontweight='bold')
    ax5.grid(True, alpha=0.3)
    ax5.set_ylim(0.5, 3.5)
    
    # Add horizontal lines for reference
    ax5.axhline(y=2.5, color='green', linestyle='--', alpha=0.7, label='Good Threshold')
    ax5.axhline(y=1.5, color='orange', linestyle='--', alpha=0.7, label='Fair Threshold')
    ax5.legend()
    
    # 7. Critical Issues and Alerts
    ax6 = fig.add_subplot(gs[4, :])
    ax6.axis('off')
    
    # Find critical issues
    critical_issues = latest_moisture[latest_moisture['condition_numeric'] < 1.5]
    moisture_issues = latest_moisture[latest_moisture['notes'].notna() & (latest_moisture['notes'] != '')]
    
    issues_text = "CRITICAL ISSUES AND ALERTS:\n\n"
    
    if not critical_issues.empty:
        issues_text += "🔴 CRITICAL MOISTURE ISSUES:\n"
        for _, row in critical_issues.iterrows():
            issues_text += f"   • {row['section']}: {row['condition']} (Score: {row['condition_numeric']:.1f})\n"
            issues_text += f"     Last Updated: {format_date_for_display(row['date'])}\n"
            if pd.notna(row['notes']) and str(row['notes']).strip():
                issues_text += f"     Note: {row['notes']}\n"
            issues_text += "\n"
    else:
        issues_text += "✅ No critical moisture issues detected.\n\n"
    
    if not moisture_issues.empty:
        issues_text += "📝 MOISTURE NOTES AND OBSERVATIONS:\n"
        for _, row in moisture_issues.iterrows():
            status_icon = "🔴" if row['condition'].lower() == 'fail' else "🟡" if row['condition'].lower() == 'partial' else "🟢"
            issues_text += f"   {status_icon} {row['section']}: {row['condition']}\n"
            issues_text += f"     Note: {row['notes']}\n"
            issues_text += f"     Date: {format_date_for_display(row['date'])}\n\n"
    
    ax6.text(0.05, 0.95, issues_text, transform=ax6.transAxes, fontsize=10,
             verticalalignment='top', fontfamily='monospace',
             bbox=dict(boxstyle='round', facecolor='lightcoral', alpha=0.8))
    
    # Save the dashboard
    plt.savefig('soil_moisture_dashboard.png', dpi=300, bbox_inches='tight')
    print("Soil Moisture Dashboard saved as 'soil_moisture_dashboard.png'")
    
    # Generate comprehensive text report
    generate_dashboard_text_report(moisture_df, latest_moisture)
    
    plt.show()

def generate_dashboard_text_report(moisture_df, latest_moisture):
    """Generate a comprehensive text report for the dashboard"""
    
    report = []
    report.append("=" * 100)
    report.append("SOIL MOISTURE DASHBOARD REPORT")
    report.append("=" * 100)
    report.append(f"Report Generated: {datetime.now().strftime('%d/%m/%Y %H:%M:%S')}")
    report.append(f"Data Range: {format_date_for_display(moisture_df['date'].min())} to {format_date_for_display(moisture_df['date'].max())}")
    report.append(f"Total Moisture Observations: {len(moisture_df)}")
    report.append("")
    
    # Executive Summary
    report.append("EXECUTIVE SUMMARY:")
    report.append("-" * 50)
    
    total_sections = latest_moisture['section'].nunique()
    good_sections = len(latest_moisture[latest_moisture['condition_numeric'] >= 2.5])
    fair_sections = len(latest_moisture[(latest_moisture['condition_numeric'] >= 1.5) & (latest_moisture['condition_numeric'] < 2.5)])
    poor_sections = len(latest_moisture[latest_moisture['condition_numeric'] < 1.5])
    
    report.append(f"• Total Sections Monitored: {total_sections}")
    report.append(f"• Good Moisture Conditions: {good_sections} sections ({good_sections/total_sections*100:.1f}%)")
    report.append(f"• Fair Moisture Conditions: {fair_sections} sections ({fair_sections/total_sections*100:.1f}%)")
    report.append(f"• Poor Moisture Conditions: {poor_sections} sections ({poor_sections/total_sections*100:.1f}%)")
    report.append("")
    
    # Section Rankings
    report.append("SECTION MOISTURE RANKINGS:")
    report.append("-" * 50)
    section_scores = latest_moisture.groupby('section')['condition_numeric'].first().sort_values(ascending=False)
    for i, (section, score) in enumerate(section_scores.items(), 1):
        status = "🟢 GOOD" if score >= 2.5 else "🟡 FAIR" if score >= 1.5 else "🔴 POOR"
        report.append(f"{i:2d}. {section}: {score:.1f} {status}")
    report.append("")
    
    # Critical Issues
    report.append("CRITICAL ISSUES REQUIRING ATTENTION:")
    report.append("-" * 50)
    critical_issues = latest_moisture[latest_moisture['condition_numeric'] < 1.5]
    if not critical_issues.empty:
        for _, row in critical_issues.iterrows():
            report.append(f"🔴 {row['section']}: {row['condition']} (Score: {row['condition_numeric']:.1f})")
            report.append(f"   Last Updated: {format_date_for_display(row['date'])}")
            if pd.notna(row['notes']) and str(row['notes']).strip():
                report.append(f"   Note: {row['notes']}")
            report.append("")
    else:
        report.append("✅ No critical moisture issues detected.")
    report.append("")
    
    # Recommendations
    report.append("RECOMMENDATIONS:")
    report.append("-" * 50)
    
    if poor_sections > 0:
        report.append("🔴 IMMEDIATE ACTIONS NEEDED:")
        report.append("• Investigate and address moisture issues in sections with 'fail' status")
        report.append("• Consider irrigation or drainage improvements for affected areas")
        report.append("• Increase monitoring frequency for critical sections")
        report.append("")
    
    if fair_sections > 0:
        report.append("🟡 MONITORING RECOMMENDATIONS:")
        report.append("• Closely monitor sections with 'partial' moisture status")
        report.append("• Implement preventive measures to avoid deterioration")
        report.append("• Consider targeted irrigation for borderline sections")
        report.append("")
    
    report.append("🟢 MAINTENANCE RECOMMENDATIONS:")
    report.append("• Continue current moisture management practices for good sections")
    report.append("• Maintain regular monitoring schedule")
    report.append("• Document successful practices for knowledge sharing")
    report.append("")
    
    # Trend Analysis
    report.append("TREND ANALYSIS:")
    report.append("-" * 50)
    
    # Calculate recent trends (last 30 days)
    latest_date = moisture_df['date'].max()
    thirty_days_ago = latest_date - timedelta(days=30)
    recent_data = moisture_df[moisture_df['date'] >= thirty_days_ago]
    
    if not recent_data.empty:
        report.append(f"• Recent observations (last 30 days): {len(recent_data)}")
        report.append(f"• Average moisture score: {recent_data['condition_numeric'].mean():.2f}")
        
        # Compare with overall average
        overall_avg = moisture_df['condition_numeric'].mean()
        recent_avg = recent_data['condition_numeric'].mean()
        if recent_avg > overall_avg:
            report.append("• Trend: Improving moisture conditions")
        elif recent_avg < overall_avg:
            report.append("• Trend: Declining moisture conditions")
        else:
            report.append("• Trend: Stable moisture conditions")
    else:
        report.append("• No recent data available for trend analysis")
    
    report.append("")
    report.append("=" * 100)
    
    # Save text report
    with open('soil_moisture_dashboard_report.txt', 'w', encoding='utf-8') as f:
        f.write('\n'.join(report))
    
    # Print report to console
    print('\n'.join(report))
    print("\nDashboard text report saved as 'soil_moisture_dashboard_report.txt'")

if __name__ == "__main__":
    create_moisture_dashboard() 
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
from datetime import datetime
import numpy as np
import base64
from io import BytesIO

def parse_date(date_str):
    """Parse date string to datetime object"""
    try:
        return datetime.strptime(date_str, '%d/%m/%Y')
    except:
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

def encode_image_to_base64(fig):
    """Convert matplotlib figure to base64 encoded string for embedding in markdown"""
    buf = BytesIO()
    fig.savefig(buf, format='png', dpi=150, bbox_inches='tight')
    buf.seek(0)
    img_str = base64.b64encode(buf.getvalue()).decode()
    buf.close()
    return img_str

def create_moisture_report(output_format='png'):
    """Create comprehensive report showing recent soil moisture conditions across sections
    
    Args:
        output_format (str): 'png', 'markdown', or 'all' for multiple outputs
    """
    
    # Read the CSV file
    df = pd.read_csv('scout.csv')
    
    # Rename the last column to 'notes' for easier access
    df.columns = list(df.columns[:-1]) + ['notes']
    
    # Parse dates
    df['date'] = df['date'].apply(parse_date)
    df = df.dropna(subset=['date'])
    
    # Filter for moisture metrics only
    moisture_df = df[df['metric'].str.lower() == 'moisture'].copy()
    
    if moisture_df.empty:
        print("No moisture data found in the CSV file.")
        return
    
    # Convert conditions to numeric values
    moisture_df['condition_numeric'] = moisture_df['condition'].apply(condition_to_numeric)
    
    # Get the most recent moisture data for each section
    latest_moisture = moisture_df.loc[moisture_df.groupby('section')['date'].idxmax()]
    
    # Create the report
    fig = plt.figure(figsize=(18, 14))
    
    # Set up the grid
    gs = fig.add_gridspec(4, 2, hspace=0.4, wspace=0.3)
    
    # 1. Current Moisture Status by Section
    ax1 = fig.add_subplot(gs[0, 0])
    
    # Sort sections by moisture score
    section_scores = latest_moisture.groupby('section')['condition_numeric'].first().sort_values(ascending=True)
    
    colors = ['red' if score < 1.5 else 'orange' if score < 2.5 else 'green' for score in section_scores.values]
    bars = ax1.barh(range(len(section_scores)), section_scores.values, color=colors)
    ax1.set_yticks(range(len(section_scores)))
    ax1.set_yticklabels(section_scores.index)
    ax1.set_xlabel('Moisture Score (1=Fail, 2=Partial, 3=Pass)')
    ax1.set_title('Current Soil Moisture Status by Section', fontsize=14, fontweight='bold')
    ax1.set_xlim(0, 3)
    ax1.grid(True, alpha=0.3)
    
    # Add value labels on bars
    for i, bar in enumerate(bars):
        width = bar.get_width()
        ax1.text(width + 0.05, bar.get_y() + bar.get_height()/2, 
                f'{width:.1f}', ha='left', va='center', fontweight='bold')
    
    # 2. Moisture Trend Over Time (Last 30 days)
    ax2 = fig.add_subplot(gs[0, 1])
    
    # Get data from last 30 days
    latest_date = moisture_df['date'].max()
    thirty_days_ago = latest_date - pd.Timedelta(days=30)
    recent_moisture = moisture_df[moisture_df['date'] >= thirty_days_ago]
    
    if not recent_moisture.empty:
        # Plot each section's moisture trend
        sections = recent_moisture['section'].unique()
        colors = plt.cm.Set3(np.linspace(0, 1, len(sections)))
        
        for i, section in enumerate(sections):
            section_data = recent_moisture[recent_moisture['section'] == section].sort_values('date')
            if not section_data.empty:
                ax2.plot(section_data['date'], section_data['condition_numeric'], 
                        marker='o', linewidth=2, markersize=6, label=section, color=colors[i])
        
        ax2.set_title('Soil Moisture Trends (Last 30 Days)', fontsize=14, fontweight='bold')
        ax2.set_xlabel('Date')
        ax2.set_ylabel('Moisture Score')
        ax2.grid(True, alpha=0.3)
        ax2.set_ylim(0.5, 3.5)
        ax2.legend(bbox_to_anchor=(1.05, 1), loc='upper left')
        plt.setp(ax2.xaxis.get_majorticklabels(), rotation=45)
    
    # 3. Moisture Condition Distribution
    ax3 = fig.add_subplot(gs[1, 0])
    
    condition_counts = latest_moisture['condition'].value_counts()
    colors = ['red', 'orange', 'green']
    wedges, texts, autotexts = ax3.pie(condition_counts.values, labels=condition_counts.index, 
                                       autopct='%1.1f%%', colors=colors, startangle=90)
    ax3.set_title('Current Moisture Condition Distribution', fontsize=12, fontweight='bold')
    
    # 4. Section Activity (Number of moisture observations per section)
    ax4 = fig.add_subplot(gs[1, 1])
    
    section_activity = moisture_df.groupby('section').size().sort_values(ascending=True)
    
    bars = ax4.barh(range(len(section_activity)), section_activity.values, color='lightblue')
    ax4.set_yticks(range(len(section_activity)))
    ax4.set_yticklabels(section_activity.index, fontsize=9)
    ax4.set_xlabel('Number of Moisture Observations')
    ax4.set_title('Section Moisture Monitoring Activity', fontsize=12, fontweight='bold')
    
    # 5. Moisture Issues and Notes
    ax5 = fig.add_subplot(gs[2, :])
    ax5.axis('off')
    
    # Find moisture issues with notes
    moisture_issues = latest_moisture[latest_moisture['notes'].notna() & (latest_moisture['notes'] != '')]
    
    issues_text = "MOISTURE ISSUES AND NOTES:\n\n"
    
    if not moisture_issues.empty:
        for _, row in moisture_issues.iterrows():
            status_icon = "🔴" if row['condition'].lower() == 'fail' else "🟡" if row['condition'].lower() == 'partial' else "🟢"
            issues_text += f"{status_icon} {row['section']}: {row['condition']}\n"
            issues_text += f"   Note: {row['notes']}\n"
            issues_text += f"   Date: {row['date'].strftime('%Y-%m-%d')}\n\n"
    else:
        issues_text += "✅ No moisture issues with notes found.\n"
    
    ax5.text(0.05, 0.95, issues_text, transform=ax5.transAxes, fontsize=11,
             verticalalignment='top', bbox=dict(boxstyle='round', facecolor='lightyellow', alpha=0.8))
    
    # 6. Detailed Section Moisture Analysis
    ax6 = fig.add_subplot(gs[3, :])
    ax6.axis('off')
    
    # Create detailed analysis for each section
    analysis_text = "DETAILED SECTION MOISTURE ANALYSIS:\n\n"
    
    for section in sorted(latest_moisture['section'].unique()):
        section_data = latest_moisture[latest_moisture['section'] == section]
        moisture_score = section_data['condition_numeric'].iloc[0]
        moisture_condition = section_data['condition'].iloc[0]
        moisture_date = section_data['date'].iloc[0]
        
        status = "🟢 GOOD" if moisture_score >= 2.5 else "🟡 FAIR" if moisture_score >= 1.5 else "🔴 POOR"
        analysis_text += f"{section}: {status} (Score: {moisture_score:.1f})\n"
        analysis_text += f"  Condition: {moisture_condition}\n"
        analysis_text += f"  Last Updated: {moisture_date.strftime('%Y-%m-%d')}\n"
        
        if pd.notna(section_data['notes'].iloc[0]) and str(section_data['notes'].iloc[0]).strip():
            analysis_text += f"  Note: {section_data['notes'].iloc[0]}\n"
        
        analysis_text += "\n"
    
    ax6.text(0.05, 0.95, analysis_text, transform=ax6.transAxes, fontsize=10,
             verticalalignment='top', bbox=dict(boxstyle='round', facecolor='lightblue', alpha=0.8))
    
    # Generate outputs based on format
    if output_format in ['png', 'all']:
        # Save the report as PNG
        plt.savefig('soil_moisture_report.png', dpi=300, bbox_inches='tight')
        print("Soil Moisture Report saved as 'soil_moisture_report.png'")
    
    if output_format in ['markdown', 'all']:
        # Generate markdown report with embedded graphics
        generate_moisture_markdown_report(moisture_df, latest_moisture, fig)
    
    # Generate text report
    generate_moisture_text_report(moisture_df, latest_moisture)
    
    if output_format == 'png':
        plt.show()

def generate_moisture_markdown_report(moisture_df, latest_moisture, fig):
    """Generate a detailed markdown report with embedded graphics for soil moisture"""
    
    # Encode the figure to base64 for embedding
    img_base64 = encode_image_to_base64(fig)
    
    report = []
    report.append("# Soil Moisture Condition Report")
    report.append("")
    report.append(f"**Report Generated:** {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}  ")
    report.append(f"**Data Range:** {moisture_df['date'].min().strftime('%Y-%m-%d')} to {moisture_df['date'].max().strftime('%Y-%m-%d')}  ")
    report.append(f"**Total Moisture Observations:** {len(moisture_df)}  ")
    report.append("")
    
    # Embedded visualization
    report.append("## 📊 Comprehensive Moisture Analysis Dashboard")
    report.append("")
    report.append(f"![Soil Moisture Dashboard](data:image/png;base64,{img_base64})")
    report.append("")
    
    # Summary Statistics
    report.append("## 📈 Summary Statistics")
    report.append("")
    report.append(f"- **Total Sections Monitored:** {latest_moisture['section'].nunique()}")
    report.append(f"- **Date Range:** {moisture_df['date'].min().strftime('%Y-%m-%d')} to {moisture_df['date'].max().strftime('%Y-%m-%d')}")
    report.append(f"- **Total Observations:** {len(moisture_df)}")
    report.append("")
    
    # Section Moisture Rankings
    report.append("## 🏆 Section Moisture Rankings (Current)")
    report.append("")
    section_scores = latest_moisture.groupby('section')['condition_numeric'].first().sort_values(ascending=False)
    
    report.append("| Rank | Section | Score | Status |")
    report.append("|------|---------|-------|--------|")
    for i, (section, score) in enumerate(section_scores.items(), 1):
        status = "🟢 GOOD" if score >= 2.5 else "🟡 FAIR" if score >= 1.5 else "🔴 POOR"
        report.append(f"| {i} | {section} | {score:.1f} | {status} |")
    report.append("")
    
    # Moisture Condition Distribution
    report.append("## 📊 Moisture Condition Distribution")
    report.append("")
    condition_counts = latest_moisture['condition'].value_counts()
    for condition, count in condition_counts.items():
        percentage = (count / len(latest_moisture)) * 100
        report.append(f"- **{condition}:** {count} sections ({percentage:.1f}%)")
    report.append("")
    
    # Moisture Issues and Notes
    report.append("## ⚠️ Moisture Issues and Notes")
    report.append("")
    moisture_issues = latest_moisture[latest_moisture['notes'].notna() & (latest_moisture['notes'] != '')]
    if not moisture_issues.empty:
        for _, row in moisture_issues.iterrows():
            status_icon = "🔴" if row['condition'].lower() == 'fail' else "🟡" if row['condition'].lower() == 'partial' else "🟢"
            report.append(f"### {status_icon} {row['section']}")
            report.append(f"- **Condition:** {row['condition']}")
            report.append(f"- **Note:** {row['notes']}")
            report.append(f"- **Date:** {row['date'].strftime('%Y-%m-%d')}")
            report.append("")
    else:
        report.append("✅ **No moisture issues with notes found.**")
    report.append("")
    
    # Detailed Section Analysis
    report.append("## 🔍 Detailed Section Moisture Analysis")
    report.append("")
    for section in sorted(latest_moisture['section'].unique()):
        section_data = latest_moisture[latest_moisture['section'] == section]
        moisture_score = section_data['condition_numeric'].iloc[0]
        moisture_condition = section_data['condition'].iloc[0]
        moisture_date = section_data['date'].iloc[0]
        
        status = "🟢 GOOD" if moisture_score >= 2.5 else "🟡 FAIR" if moisture_score >= 1.5 else "🔴 POOR"
        report.append(f"### {section}")
        report.append(f"- **Status:** {status} (Score: {moisture_score:.1f})")
        report.append(f"- **Condition:** {moisture_condition}")
        report.append(f"- **Last Updated:** {moisture_date.strftime('%Y-%m-%d')}")
        
        if pd.notna(section_data['notes'].iloc[0]) and str(section_data['notes'].iloc[0]).strip():
            report.append(f"- **Note:** {section_data['notes'].iloc[0]}")
        
        report.append("")
    
    # Key Insights
    report.append("## 💡 Key Insights")
    report.append("")
    
    # Calculate insights
    good_sections = len(latest_moisture[latest_moisture['condition_numeric'] >= 2.5])
    fair_sections = len(latest_moisture[latest_moisture['condition_numeric'] >= 1.5]) - good_sections
    poor_sections = len(latest_moisture) - good_sections - fair_sections
    
    report.append(f"- **Good Condition:** {good_sections} sections ({good_sections/len(latest_moisture)*100:.1f}%)")
    report.append(f"- **Fair Condition:** {fair_sections} sections ({fair_sections/len(latest_moisture)*100:.1f}%)")
    report.append(f"- **Poor Condition:** {poor_sections} sections ({poor_sections/len(latest_moisture)*100:.1f}%)")
    report.append("")
    
    # Recommendations
    report.append("## 🎯 Recommendations")
    report.append("")
    if poor_sections > 0:
        report.append("🔴 **Immediate Action Required:**")
        report.append("- Investigate sections with poor moisture conditions")
        report.append("- Consider irrigation or drainage improvements")
        report.append("- Monitor these sections more frequently")
        report.append("")
    
    if fair_sections > 0:
        report.append("🟡 **Monitor Closely:**")
        report.append("- Keep track of sections with fair moisture conditions")
        report.append("- Prepare intervention plans if conditions worsen")
        report.append("")
    
    if good_sections > 0:
        report.append("🟢 **Maintain Standards:**")
        report.append("- Continue current management practices for good sections")
        report.append("- Use as benchmarks for improvement in other sections")
        report.append("")
    
    report.append("---")
    report.append(f"*Report generated on {datetime.now().strftime('%Y-%m-%d at %H:%M:%S')}*")
    
    # Save markdown report
    with open('soil_moisture_report.md', 'w', encoding='utf-8') as f:
        f.write('\n'.join(report))
    
    print("Markdown report saved as 'soil_moisture_report.md'")

def generate_moisture_text_report(moisture_df, latest_moisture):
    """Generate a detailed text report for soil moisture"""
    
    report = []
    report.append("=" * 80)
    report.append("SOIL MOISTURE CONDITION REPORT")
    report.append("=" * 80)
    report.append(f"Report Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    report.append(f"Data Range: {moisture_df['date'].min().strftime('%Y-%m-%d')} to {moisture_df['date'].max().strftime('%Y-%m-%d')}")
    report.append(f"Total Moisture Observations: {len(moisture_df)}")
    report.append("")
    
    # Summary Statistics
    report.append("SUMMARY STATISTICS:")
    report.append("-" * 40)
    report.append(f"Total Sections Monitored: {latest_moisture['section'].nunique()}")
    report.append(f"Date Range: {moisture_df['date'].min().strftime('%Y-%m-%d')} to {moisture_df['date'].max().strftime('%Y-%m-%d')}")
    report.append("")
    
    # Section Moisture Rankings
    report.append("SECTION MOISTURE RANKINGS (Current):")
    report.append("-" * 40)
    section_scores = latest_moisture.groupby('section')['condition_numeric'].first().sort_values(ascending=False)
    for i, (section, score) in enumerate(section_scores.items(), 1):
        status = "🟢 GOOD" if score >= 2.5 else "🟡 FAIR" if score >= 1.5 else "🔴 POOR"
        report.append(f"{i:2d}. {section}: {score:.1f} {status}")
    report.append("")
    
    # Moisture Condition Distribution
    report.append("MOISTURE CONDITION DISTRIBUTION:")
    report.append("-" * 40)
    condition_counts = latest_moisture['condition'].value_counts()
    for condition, count in condition_counts.items():
        percentage = (count / len(latest_moisture)) * 100
        report.append(f"• {condition}: {count} sections ({percentage:.1f}%)")
    report.append("")
    
    # Moisture Issues and Notes
    report.append("MOISTURE ISSUES AND NOTES:")
    report.append("-" * 40)
    moisture_issues = latest_moisture[latest_moisture['notes'].notna() & (latest_moisture['notes'] != '')]
    if not moisture_issues.empty:
        for _, row in moisture_issues.iterrows():
            status_icon = "🔴" if row['condition'].lower() == 'fail' else "🟡" if row['condition'].lower() == 'partial' else "🟢"
            report.append(f"{status_icon} {row['section']}: {row['condition']}")
            report.append(f"   Note: {row['notes']}")
            report.append(f"   Date: {row['date'].strftime('%Y-%m-%d')}")
            report.append("")
    else:
        report.append("✅ No moisture issues with notes found.")
    report.append("")
    
    # Detailed Section Analysis
    report.append("DETAILED SECTION MOISTURE ANALYSIS:")
    report.append("-" * 40)
    for section in sorted(latest_moisture['section'].unique()):
        section_data = latest_moisture[latest_moisture['section'] == section]
        moisture_score = section_data['condition_numeric'].iloc[0]
        moisture_condition = section_data['condition'].iloc[0]
        moisture_date = section_data['date'].iloc[0]
        
        status = "🟢 GOOD" if moisture_score >= 2.5 else "🟡 FAIR" if moisture_score >= 1.5 else "🔴 POOR"
        report.append(f"\n{section}: {status} (Score: {moisture_score:.1f})")
        report.append(f"  Condition: {moisture_condition}")
        report.append(f"  Last Updated: {moisture_date.strftime('%Y-%m-%d')}")
        
        if pd.notna(section_data['notes'].iloc[0]) and str(section_data['notes'].iloc[0]).strip():
            report.append(f"  Note: {section_data['notes'].iloc[0]}")
    
    report.append("")
    report.append("=" * 80)
    
    # Save text report
    with open('soil_moisture_report.txt', 'w', encoding='utf-8') as f:
        f.write('\n'.join(report))
    
    # Print report to console
    print('\n'.join(report))
    print("\nText report saved as 'soil_moisture_report.txt'")

if __name__ == "__main__":
    import sys
    
    # Check command line arguments for output format
    output_format = 'png'  # default
    if len(sys.argv) > 1:
        output_format = sys.argv[1].lower()
        if output_format not in ['png', 'markdown', 'all']:
            print("Usage: python moisture_report.py [png|markdown|all]")
            print("  png: Generate PNG image only")
            print("  markdown: Generate markdown report with embedded graphics")
            print("  all: Generate both PNG and markdown")
            sys.exit(1)
    
    print(f"Generating moisture report in {output_format} format...")
    create_moisture_report(output_format) 
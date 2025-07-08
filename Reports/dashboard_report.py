import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
from datetime import datetime
import numpy as np
from collections import defaultdict

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

def create_dashboard_report():
    """Create comprehensive dashboard report showing section health by section and metric"""
    
    # Read the CSV file
    df = pd.read_csv('scout.csv')
    
    # Rename the last column to 'notes' for easier access
    df.columns = list(df.columns[:-1]) + ['notes']
    
    # Parse dates
    df['date'] = df['date'].apply(parse_date)
    df = df.dropna(subset=['date'])
    
    # Convert conditions to numeric values
    df['condition_numeric'] = df['condition'].apply(condition_to_numeric)
    
    # Get the most recent date for each section
    latest_data = df.loc[df.groupby('section')['date'].idxmax()]
    
    # Create the dashboard
    fig = plt.figure(figsize=(20, 16))
    
    # Set up the grid
    gs = fig.add_gridspec(4, 3, hspace=0.3, wspace=0.3)
    
    # 1. Current Health Status by Section (Heatmap)
    ax1 = fig.add_subplot(gs[0, :2])
    
    # Create pivot table for current conditions
    current_pivot = latest_data.pivot_table(
        index='section', 
        columns='metric', 
        values='condition_numeric', 
        aggfunc='first'
    )
    
    # Create heatmap
    sns.heatmap(current_pivot, annot=True, cmap='RdYlGn', center=2, 
                cbar_kws={'label': 'Condition (1=Fail, 2=Partial, 3=Pass)'}, ax=ax1)
    ax1.set_title('Current Health Status by Section and Metric', fontsize=14, fontweight='bold')
    ax1.set_xlabel('Metric')
    ax1.set_ylabel('Section')
    
    # 2. Overall Section Health Score
    ax2 = fig.add_subplot(gs[0, 2])
    
    # Calculate average health score per section
    section_scores = latest_data.groupby('section')['condition_numeric'].mean().sort_values(ascending=True)
    
    bars = ax2.barh(range(len(section_scores)), section_scores.values, color='skyblue')
    ax2.set_yticks(range(len(section_scores)))
    ax2.set_yticklabels(section_scores.index)
    ax2.set_xlabel('Average Health Score')
    ax2.set_title('Overall Section Health Score\n(Current)', fontsize=12, fontweight='bold')
    ax2.set_xlim(0, 3)
    
    # Add value labels on bars
    for i, bar in enumerate(bars):
        width = bar.get_width()
        ax2.text(width + 0.05, bar.get_y() + bar.get_height()/2, 
                f'{width:.1f}', ha='left', va='center')
    
    # 3. Metric Performance Distribution
    ax3 = fig.add_subplot(gs[1, 0])
    
    metric_performance = latest_data.groupby('metric')['condition_numeric'].mean().sort_values(ascending=True)
    
    bars = ax3.barh(range(len(metric_performance)), metric_performance.values, color='lightgreen')
    ax3.set_yticks(range(len(metric_performance)))
    ax3.set_yticklabels(metric_performance.index, fontsize=8)
    ax3.set_xlabel('Average Score')
    ax3.set_title('Metric Performance\n(Current)', fontsize=12, fontweight='bold')
    ax3.set_xlim(0, 3)
    
    # 4. Condition Distribution
    ax4 = fig.add_subplot(gs[1, 1])
    
    condition_counts = latest_data['condition'].value_counts()
    colors = ['red', 'orange', 'green']
    ax4.pie(condition_counts.values, labels=condition_counts.index, autopct='%1.1f%%', 
            colors=colors, startangle=90)
    ax4.set_title('Current Condition Distribution', fontsize=12, fontweight='bold')
    
    # 5. Section Activity (Number of metrics monitored)
    ax5 = fig.add_subplot(gs[1, 2])
    
    section_activity = latest_data.groupby('section').size().sort_values(ascending=True)
    
    bars = ax5.barh(range(len(section_activity)), section_activity.values, color='lightcoral')
    ax5.set_yticks(range(len(section_activity)))
    ax5.set_yticklabels(section_activity.index, fontsize=8)
    ax5.set_xlabel('Number of Metrics Monitored')
    ax5.set_title('Section Activity\n(Metrics Monitored)', fontsize=12, fontweight='bold')
    
    # 6. Time Series of Health Trends (Last 30 days)
    ax6 = fig.add_subplot(gs[2, :])
    
    # Get data from last 30 days
    latest_date = df['date'].max()
    thirty_days_ago = latest_date - pd.Timedelta(days=30)
    recent_data = df[df['date'] >= thirty_days_ago]
    
    if not recent_data.empty:
        # Calculate daily average health score
        daily_health = recent_data.groupby('date')['condition_numeric'].mean()
        
        ax6.plot(daily_health.index, daily_health.values, marker='o', linewidth=2, markersize=6)
        ax6.set_title('Overall Health Trend (Last 30 Days)', fontsize=14, fontweight='bold')
        ax6.set_xlabel('Date')
        ax6.set_ylabel('Average Health Score')
        ax6.grid(True, alpha=0.3)
        ax6.set_ylim(0, 3)
        
        # Add trend line
        z = np.polyfit(range(len(daily_health)), daily_health.values, 1)
        p = np.poly1d(z)
        ax6.plot(daily_health.index, p(range(len(daily_health))), "r--", alpha=0.8, label='Trend')
        ax6.legend()
    
    # 7. Critical Issues Summary
    ax7 = fig.add_subplot(gs[3, :])
    ax7.axis('off')
    
    # Find sections with fail conditions
    fail_issues = latest_data[latest_data['condition'].str.lower() == 'fail']
    
    if not fail_issues.empty:
        critical_text = "CRITICAL ISSUES REQUIRING ATTENTION:\n\n"
        for _, row in fail_issues.iterrows():
            critical_text += f"• {row['section']} - {row['metric']}: {row['condition']}\n"
            if pd.notna(row['notes']) and str(row['notes']).strip():
                critical_text += f"  Note: {row['notes']}\n"
    else:
        critical_text = "✅ NO CRITICAL ISSUES DETECTED\nAll sections are currently in acceptable condition."
    
    ax7.text(0.05, 0.95, critical_text, transform=ax7.transAxes, fontsize=11,
             verticalalignment='top', bbox=dict(boxstyle='round', facecolor='lightyellow', alpha=0.8))
    
    # Save the dashboard
    plt.savefig('section_health_dashboard.png', dpi=300, bbox_inches='tight')
    print("Dashboard saved as 'section_health_dashboard.png'")
    
    # Generate text report
    generate_text_report(df, latest_data)
    
    plt.show()

def generate_text_report(df, latest_data):
    """Generate a detailed text report"""
    
    report = []
    report.append("=" * 80)
    report.append("FARM SECTION HEALTH DASHBOARD REPORT")
    report.append("=" * 80)
    report.append(f"Report Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    report.append(f"Data Range: {df['date'].min().strftime('%Y-%m-%d')} to {df['date'].max().strftime('%Y-%m-%d')}")
    report.append("")
    
    # Summary Statistics
    report.append("SUMMARY STATISTICS:")
    report.append("-" * 40)
    report.append(f"Total Sections Monitored: {latest_data['section'].nunique()}")
    report.append(f"Total Metrics Tracked: {latest_data['metric'].nunique()}")
    report.append(f"Total Observations: {len(df)}")
    report.append("")
    
    # Section Health Rankings
    report.append("SECTION HEALTH RANKINGS (Current):")
    report.append("-" * 40)
    section_scores = latest_data.groupby('section')['condition_numeric'].mean().sort_values(ascending=False)
    for i, (section, score) in enumerate(section_scores.items(), 1):
        status = "🟢 Good" if score >= 2.5 else "🟡 Fair" if score >= 1.5 else "🔴 Poor"
        report.append(f"{i:2d}. {section}: {score:.2f} {status}")
    report.append("")
    
    # Metric Performance
    report.append("METRIC PERFORMANCE (Current):")
    report.append("-" * 40)
    metric_scores = latest_data.groupby('metric')['condition_numeric'].mean().sort_values(ascending=False)
    for metric, score in metric_scores.items():
        status = "🟢 Good" if score >= 2.5 else "🟡 Fair" if score >= 1.5 else "🔴 Poor"
        report.append(f"• {metric}: {score:.2f} {status}")
    report.append("")
    
    # Critical Issues
    report.append("CRITICAL ISSUES:")
    report.append("-" * 40)
    fail_issues = latest_data[latest_data['condition'].str.lower() == 'fail']
    if not fail_issues.empty:
        for _, row in fail_issues.iterrows():
            report.append(f"🔴 {row['section']} - {row['metric']}: {row['condition']}")
            if pd.notna(row['notes']) and str(row['notes']).strip():
                report.append(f"   Note: {row['notes']}")
    else:
        report.append("✅ No critical issues detected")
    report.append("")
    
    # Recommendations
    report.append("RECOMMENDATIONS:")
    report.append("-" * 40)
    
    # Find sections with lowest scores
    low_score_sections = section_scores[section_scores < 2.0]
    if not low_score_sections.empty:
        report.append("Priority Actions Required:")
        for section in low_score_sections.index:
            section_issues = latest_data[(latest_data['section'] == section) & 
                                       (latest_data['condition_numeric'] < 2)]
            report.append(f"• {section}: Focus on {', '.join(section_issues['metric'].tolist())}")
    
    # Find metrics with lowest scores
    low_score_metrics = metric_scores[metric_scores < 2.0]
    if not low_score_metrics.empty:
        report.append("\nGeneral Improvements Needed:")
        for metric in low_score_metrics.index:
            report.append(f"• {metric}: Consider farm-wide improvement strategies")
    
    report.append("")
    report.append("=" * 80)
    
    # Save text report
    with open('section_health_report.txt', 'w', encoding='utf-8') as f:
        f.write('\n'.join(report))
    
    # Print report to console
    print('\n'.join(report))
    print("\nText report saved as 'section_health_report.txt'")

if __name__ == "__main__":
    create_dashboard_report() 
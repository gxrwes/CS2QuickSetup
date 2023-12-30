GeExpression;t-PSDrive -PSProvider FileSystem | Select-Object -Property @{Name="Used(GB)";Expression={"{0:N2}" -f ($_.Used / 1GB)}}, @{Name="Free(GB)"; Expression={"{0}"}}

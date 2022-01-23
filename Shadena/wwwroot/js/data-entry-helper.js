window.getActiveDataType = function (prefix) {
  var tabListElement = document.getElementById(prefix + "-type-tab");
  
  if (!tabListElement)
      return null;
  
  var children = tabListElement.children;
  
  for (let i = 0; i < children.length; i++) {
      const child = children[i];
      if (!child.children)
          continue;
      
      const realChild = child.children[0];
      
      if (realChild.classList.contains('active')) {
          const childIdParts = realChild.id.split('-');
          return childIdParts[childIdParts.length - 2];
      }
  }
};

window.setActiveDataType = function (prefix, type) {
    var tabListElement = document.getElementById(prefix + "-type-tab");

    if (!tabListElement)
        return;

    var children = tabListElement.children;

    for (let i = 0; i < children.length; i++) {
        const child = children[i];
        if (!child.children)
            continue;

        const realChild = child.children[0];
        const childIdParts = realChild.id.split('-');
        const tabType = childIdParts[childIdParts.length - 2];
        
        if (tabType === type) {
            bootstrap.Tab.getOrCreateInstance(realChild).show();
            return;
        }
    }
}
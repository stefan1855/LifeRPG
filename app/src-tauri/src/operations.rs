//! A root switch or maintenance operation must not overlap database/file access.
use std::sync::{RwLock, RwLockReadGuard, RwLockWriteGuard};
static DATA: RwLock<()> = RwLock::new(());
pub fn read() -> Result<RwLockReadGuard<'static, ()>, String> {
    DATA.try_read().map_err(|_| "数据维护中，请稍后重试".into())
}
pub fn write() -> Result<RwLockWriteGuard<'static, ()>, String> {
    DATA.try_write().map_err(|_| "还有数据操作正在进行，请稍后重试维护".into())
}
